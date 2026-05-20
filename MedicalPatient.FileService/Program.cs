using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using MassTransit;
using MedicalPatient.AppHost.ServiceDefaults;
using MedicalPatient.FileService;
using MedicalPatient.FileService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var useLocalStack = builder.Configuration.GetValue<bool>("LocalStack:UseLocalStack");
var localStackUrl = builder.Configuration["LocalStack:LocalStackUrl"] ?? "http://localhost:4566";
var accessKey = builder.Configuration["LocalStack:AwsAccessKeyId"] ?? "admin";
var secretKey = builder.Configuration["LocalStack:AwsSecretAccessKey"] ?? "admin";
var region = builder.Configuration["LocalStack:AwsRegion"] ?? "us-east-1";

var bucketName = BucketNameResolver.Resolve(builder.Configuration["S3:BucketName"]);

// Настройка S3 клиента
if (useLocalStack)
{
    builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
        new BasicAWSCredentials(accessKey, secretKey),
        new AmazonS3Config
        {
            ServiceURL = localStackUrl,
            ForcePathStyle = true,
            UseHttp = true,
            AuthenticationRegion = region
        }
    ));
}
else
{
    builder.Services.AddAWSService<IAmazonS3>(new AWSOptions
    {
        Region = RegionEndpoint.GetBySystemName(region)
    });
}

builder.Services.AddSingleton(bucketName);

var sqsServiceUrl = builder.Configuration["SQS:ServiceUrl"] ?? "http://localhost:4566";
var queueName = builder.Configuration["SQS:QueueName"] ?? "medical-patients";

builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(
    new BasicAWSCredentials(accessKey, secretKey),
    new AmazonSQSConfig
    {
        ServiceURL = sqsServiceUrl,
        AuthenticationRegion = region,
        UseHttp = true
    }
));

builder.Services.AddHostedService<SQSService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PatientConsumer>();

    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(region, h =>
        {
            h.AccessKey(accessKey);
            h.SecretKey(secretKey);

            if (useLocalStack)
            {
                h.Config(new AmazonSQSConfig
                {
                    ServiceURL = sqsServiceUrl,
                    AuthenticationRegion = region,
                    UseHttp = true
                });
            }
        });

        cfg.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType);

        cfg.ReceiveEndpoint(queueName, e =>
        {
            e.ConfigureConsumeTopology = false;
            e.UseRawJsonDeserializer(RawSerializerOptions.AnyMessageType);
            e.Consumer<PatientConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<S3Initializer>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();