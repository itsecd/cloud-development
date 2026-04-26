using Amazon.SQS;
using AspireApp.ApiService.Generator;
using AspireApp.ApiService.Messaging;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddScoped<SqsProducerService>();

builder.Services.AddScoped<IWarehouseCache, WarehouseCache>();
builder.Services.AddSingleton<WarehouseGenerator>();
builder.Services.AddScoped<IWarehouseGeneratorService, WarehouseGeneratorService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/warehouse", async (IWarehouseGeneratorService service, int id) =>
{
    var warehouse = await service.ProcessWarehouse(id);
    return Results.Ok(warehouse);
});

app.Run();
