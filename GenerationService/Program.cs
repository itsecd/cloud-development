using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using GenerationService.Options;
using GenerationService.Services;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new CompactJsonFormatter()));

// Redis
builder.AddRedisDistributedCache("redis");

// === AWS Configuration ===
var awsUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
var credentials = new BasicAWSCredentials("test", "test");

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(
    _ => new AmazonSimpleNotificationServiceClient(credentials,
        new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = awsUrl
        }));

// Сервисы
builder.Services.AddSingleton<ContractGeneratorService>();
builder.Services.AddSingleton<ContractCacheService>();
builder.Services.AddSingleton<SnsPublisherService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection("CacheOptions"));

var app = builder.Build();

var replicaName = builder.Configuration["REPLICA_NAME"] ?? "unknown";

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/contracts/{id:int}", async (
    int id,
    ContractCacheService cacheService,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Request handled by replica {ReplicaName} for ID: {Id}",
        replicaName, id);

    var contract = await cacheService.GetOrCreateAsync(id);
    return Results.Ok(contract);
});

app.MapDefaultEndpoints();

app.Run();