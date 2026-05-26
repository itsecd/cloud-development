using System;
using System.Text;
using System.Text.Json;
using ProjectApp.Domain.Entities;
using ProjectApp.Domain.Messaging;
using RabbitMQ.Client;

namespace ProjectApp.Api.Services.Messaging;

public sealed class RabbitMqPatientGeneratedPublisher(
    IConfiguration configuration,
    ILogger<RabbitMqPatientGeneratedPublisher> logger) : IPatientGeneratedPublisher, IAsyncDisposable
{
    private readonly PatientMessagingOptions _options =
        configuration.GetSection(PatientMessagingOptions.SectionName).Get<PatientMessagingOptions>() ?? new();

    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(MedicalPatient patient, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            var message = new PatientGeneratedMessage
            {
                Patient = patient,
                GeneratedAt = DateTimeOffset.UtcNow
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: _options.RoutingKey,
                mandatory: false,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation("Published patient {Id} to {ExchangeName}", patient.Id, _options.ExchangeName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish patient {Id} to RabbitMQ", patient.Id);
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            _connection = await CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            return _channel;
        }
        finally
        {
            _connectionLock.Release();
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

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }
}
