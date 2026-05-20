//using MedicalPatient.Generator.Services;
//using MedicalPatient.Generator;
//using MedicalPatient.AppHost.ServiceDefaults;
//using Serilog;
//using Amazon.SQS;
//using Amazon.Runtime;
//using System.Text.Json;
//using Amazon.SQS.Model;

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

//builder.AddRedisDistributedCache("redis");

//builder.Services.AddSingleton<MedicalPatientGenerator>();
//builder.Services.AddScoped<MedicalPatientService>();

//var sqsServiceUrl = builder.Configuration["SQS:ServiceUrl"] ?? "http://localhost:9324";
//var queueName = builder.Configuration["SQS:QueueName"] ?? "medical-patients";

//// Регистрируем SQS клиент напрямую (без MassTransit для отправки)
//builder.Services.AddSingleton<IAmazonSQS>(sp =>
//{
//    var config = new AmazonSQSConfig
//    {
//        ServiceURL = sqsServiceUrl,
//        AuthenticationRegion = "us-east-1",
//        UseHttp = true
//    };

//    var credentials = new BasicAWSCredentials("admin", "admin");
//    return new AmazonSQSClient(credentials, config);
//});

//Log.Logger = new LoggerConfiguration()
//    .WriteTo.OpenTelemetry()
//    .WriteTo.Console()
//    .CreateLogger();

//var app = builder.Build();

//app.UseSerilogRequestLogging();

//app.MapDefaultEndpoints();

//app.MapGet("/medicalpatient-generator", async (
//    int id,
//    MedicalPatientService service,
//    IAmazonSQS sqsClient,
//    ILogger<Program> logger,
//    CancellationToken cancellationToken) =>
//{
//    logger.LogInformation("A request was received for a company employee with the ID: {Id}", id);

//    if (id <= 0)
//    {
//        logger.LogWarning("Invalid ID: {Id}", id);
//        return Results.BadRequest(new { error = "ID must be > 0" });
//    }

//    try
//    {
//        var application = await service.GetByIdAsync(id, cancellationToken);

//        if (application == null)
//        {
//            logger.LogWarning("Medical patient with ID {Id} not found", id);
//            return Results.NotFound(new { error = $"Patient with ID {id} not found" });
//        }

//        // Получаем URL очереди
//        var getQueueUrlRequest = new GetQueueUrlRequest
//        {
//            QueueName = queueName
//        };

//        string queueUrl;
//        try
//        {
//            var getQueueUrlResponse = await sqsClient.GetQueueUrlAsync(getQueueUrlRequest, cancellationToken);
//            queueUrl = getQueueUrlResponse.QueueUrl;
//            logger.LogInformation("Queue URL: {QueueUrl}", queueUrl);
//        }
//        catch (AmazonSQSException ex) when (ex.ErrorCode == "AWS.SimpleQueueService.NonExistentQueue")
//        {
//            // Очередь не существует, создаем её
//            logger.LogInformation("Queue {QueueName} does not exist, creating...", queueName);
//            var createQueueRequest = new CreateQueueRequest
//            {
//                QueueName = queueName
//            };
//            var createQueueResponse = await sqsClient.CreateQueueAsync(createQueueRequest, cancellationToken);
//            queueUrl = createQueueResponse.QueueUrl;
//            logger.LogInformation("Queue created at: {QueueUrl}", queueUrl);
//        }

//        // Создаем сообщение
//        var message = new MedicalPatientMessage(
//            application.Id,
//            application.FullName,
//            application.Address,
//            application.BirthDate,
//            application.Height,
//            application.Weight,
//            application.BloodType,
//            application.RhFactor,
//            application.LastInspectionDate,
//            application.VaccinationMark
//        );

//        var messageBody = JsonSerializer.Serialize(message);

//        var sendRequest = new SendMessageRequest
//        {
//            QueueUrl = queueUrl,
//            MessageBody = messageBody
//        };

//        await sqsClient.SendMessageAsync(sendRequest, cancellationToken);

//        logger.LogInformation("Successfully sent patient {Id} to queue {QueueName}", application.Id, queueName);

//        return Results.Ok(application);
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "Error when receiving data about a medical patient with {Id}", id);
//        return Results.Problem($"An error occurred while processing the request: {ex.Message}");
//    }
//})
//.WithName("GetMedicalPatient");

//app.Run();