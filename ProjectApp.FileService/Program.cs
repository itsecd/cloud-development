using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using ProjectApp.FileService;
using ProjectApp.FileService.Messaging;
using ProjectApp.FileService.Options;
using ProjectApp.FileService.Storage;
using ProjectApp.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.Configure<FilePersistenceOptions>(
    builder.Configuration.GetSection(FilePersistenceOptions.SectionName));

var serviceUrl = builder.Configuration["Services:localstack:HttpEndpoint"] ?? "http://localhost:4566";
var credentials = new BasicAWSCredentials("test", "test");

builder.Services.AddAWSService<IAmazonSQS>(new AWSOptions
{
    Credentials = credentials,
    Region = RegionEndpoint.USEast1,
    DefaultClientConfig =
    {
        ServiceURL = serviceUrl,
        AuthenticationRegion = "us-east-1"
    }
});
builder.Services.AddSingleton<IAmazonS3>(_ =>
    new AmazonS3Client(credentials, new AmazonS3Config
    {
        ServiceURL = serviceUrl,
        AuthenticationRegion = "us-east-1",
        ForcePathStyle = true
    }));

builder.Services.AddSingleton<ICreditApplicationEventConsumer, SqsCreditApplicationEventConsumer>();
builder.Services.AddSingleton<ICreditApplicationObjectStorage, S3CreditApplicationObjectStorage>();
builder.Services.AddHostedService<CreditApplicationFilePersistenceWorker>();

var host = builder.Build();
host.Run();
