using Amazon.SimpleNotificationService;
using CreditApp.Api.Messaging;
using CreditApp.Api.Services;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddScoped<IProducerService, SnsPublisherService>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

builder.Services.AddSingleton<CreditApplicationGenerator>();
builder.Services.AddScoped<ICreditApplicationService, CreditApplicationService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/credit-application", async (int id, ICreditApplicationService service) =>
    await service.GetOrGenerate(id));

app.Run();
