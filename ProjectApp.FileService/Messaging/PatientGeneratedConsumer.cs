using System.Text.Json;
using ProjectApp.Domain.Messaging;
using ProjectApp.FileService.ObjectStorage;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ProjectApp.FileService.Messaging;

public sealed class PatientGeneratedConsumer(
    IConfiguration configuration,
    IPatientFileStorage fileStorage,
    ILogger<PatientGeneratedConsumer> logger) : BackgroundService
{
    private readonly PatientMessagingOptions _options =
        configuration.GetSection(PatientMessagingOptions.SectionName).Get<PatientMessagingOptions>() ?? new();

    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartConsumerAsync(stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ consumer failed. Retrying in 5 seconds");
                await DisposeRabbitMqAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task StartConsumerAsync(CancellationToken cancellationToken)
    {
        _connection = await CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: _options.RoutingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation("Started RabbitMQ consumer for queue {QueueName}", _options.QueueName);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var message = JsonSerializer.Deserialize<PatientGeneratedMessage>(args.Body.Span);
            if (message is null)
            {
                throw new JsonException("Patient generated message is empty");
            }

            await fileStorage.SaveAsync(message);
            await _channel.BasicAckAsync(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process generated patient message");
            await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("messaging")
            ?? configuration["RabbitMQ:ConnectionString"]
            ?? "amqp://guest:guest@localhost:5672";

        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        };

        return await factory.CreateConnectionAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeRabbitMqAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task DisposeRabbitMqAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
