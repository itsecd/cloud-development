using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using MedicalPatient.FileService;
using MedicalPatient.FileService.Services;
using MedicalPatient.AppHost.ServiceDefaults;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var minioSettings = builder.Configuration.GetSection("MinIO");
var minioConfiguration = minioSettings.Get<MinioConfiguration>() ?? new MinioConfiguration();

var minioAccessKey = minioConfiguration.AccessKey;
var minioSecretKey = minioConfiguration.SecretKey;
var minioEndpoint = minioConfiguration.Endpoint;

builder.Services.Configure<MinioConfiguration>(minioSettings);

builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
    new BasicAWSCredentials(minioAccessKey, minioSecretKey),
    new AmazonS3Config
    {
        ServiceURL = minioEndpoint,
        ForcePathStyle = true,
        AuthenticationRegion = "us-east-1"
    }
));

var sqsServiceUrl = builder.Configuration["SQS:ServiceUrl"] ?? "http://localhost:9324";
var queueName = builder.Configuration["SQS:QueueName"] ?? "medical-patients";

builder.Services.AddHostedService<SQSService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PatientConsumer>();

    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host("us-east-1", h =>
        {
            h.AccessKey("test");
            h.SecretKey("test");
            h.Config(new AmazonSQSConfig
            {
                ServiceURL = sqsServiceUrl,
                AuthenticationRegion = "us-east-1"
            });
        });

        cfg.ReceiveEndpoint(queueName, e =>
        {
            e.ConfigureConsumeTopology = false;
            e.UseRawJsonDeserializer(RawSerializerOptions.AnyMessageType);
            e.Consumer<PatientConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<MinioInitializer>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
