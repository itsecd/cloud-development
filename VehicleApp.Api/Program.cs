using Amazon.SQS;
using LocalStack.Client.Extensions;
using VehicleApp.Api.Services;
using VehicleApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddScoped<IVehicleProducer, SqsVehicleProducer>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/vehicle", async (int id, IVehicleService service) =>
    Results.Ok(await service.GetOrGenerateAsync(id)));

app.Run();
