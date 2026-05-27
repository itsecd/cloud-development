using Amazon.Runtime;
using Amazon.SQS;
using VehicleGen.Api.Services;

Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");
Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", "eu-central-1");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IVehicleGenerator, VehicleGenerator>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

builder.AddRedisDistributedCache("cache");

var sqsConfig = new AmazonSQSConfig
{
    ServiceURL = "http://localhost:14566",
    UseHttp = true,
    AuthenticationRegion = "eu-central-1"
};
var credentials = new BasicAWSCredentials("test", "test");
builder.Services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(credentials, sqsConfig));
builder.Services.AddSingleton<IVehiclePublisherService, SqsVehiclePublisherService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();

app.MapGet("/api/vehicle", async (int id, IVehicleGenerator generator, ICacheService cache, IConfiguration config) =>
{
    if (id <= 0)
        return Results.BadRequest("ID должен быть больше нуля");

    var cached = await cache.RetrieveVehicleAsync(id);
    if (cached is not null)
        return Results.Ok(cached);

    var newVehicle = generator.CreateVehicle(id);
    var ttl = config.GetValue<int>("Cache:ExpirationMinutes", 5);
    await cache.StoreVehicleAsync(newVehicle, ttl);

    return Results.Ok(newVehicle);
});

app.Run();