using Vehicle.Api.Cache;
using Vehicle.Api.Generation;
using Vehicle.Api.Services;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Vehicle.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<VehicleGenerator>();
builder.Services.AddScoped<IVehicleCache, RedisVehicleCache>();
builder.Services.AddScoped<VehicleService>();

builder.Services.Configure<SnsOptions>(options =>
{
    options.TopicArn =
        builder.Configuration["AWS:Resources:SNSTopicArn"]
        ?? "arn:aws:sns:us-east-1:000000000000:vehicle-generated";
});

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    var serviceUrl = configuration["AWS:ServiceUrl"] ?? "http://localhost:4566";
    var region = configuration["AWS:Region"] ?? "us-east-1";

    var config = new AmazonSimpleNotificationServiceConfig
    {
        ServiceURL = serviceUrl,
        AuthenticationRegion = region
    };

    return new AmazonSimpleNotificationServiceClient(
        new BasicAWSCredentials("test", "test"),
        config);
});

builder.Services.AddSingleton<IProducerService, SnsPublisherService>();

builder.AddRedisDistributedCache("redis");

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "vehicle-api-unknown";
    context.Response.Headers["X-Instance-Id"] = instanceId;
    await next();
});

app.MapGet("/", () =>
{
    var instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "vehicle-api-unknown";

    return Results.Ok(new
    {
        service = "Vehicle.Api",
        status = "ok",
        instanceId,
        message = "Vehicle API is running"
    });
});

app.MapControllers();

app.Run();