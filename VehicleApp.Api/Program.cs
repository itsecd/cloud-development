using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using VehicleApp.Api.Services;
using VehicleApp.Api.Services.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
builder.Services.AddScoped<IVehiclePublisher, SnsVehiclePublisher>();

builder.Services.AddScoped<IVehicleService, VehicleService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/vehicles", async (int id, IVehicleService vehicleService) =>
{
    var vehicle = await vehicleService.GetVehicle(id);
    return Results.Ok(vehicle);
});

app.Run();
