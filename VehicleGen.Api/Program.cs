using Amazon.Runtime;
using Amazon.SQS;
using VehicleGen.Api.Services;

Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");
Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", "eu-central-1");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IVehicleGenerator, VehicleGenerator>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithMethods("GET");
    });
});

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();

app.MapGet("/api/vehicle", async (int id, IVehicleService vehicleService) =>
{
    try
    {
        var vehicle = await vehicleService.GetOrCreateVehicleAsync(id);
        return Results.Ok(vehicle);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();