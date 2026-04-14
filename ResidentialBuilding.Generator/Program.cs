using Amazon.SimpleNotificationService;
using Generator.Generator;
using Generator.Messaging;
using Generator.Service;
using LocalStack.Client.Extensions;
using ResidentialBuilding.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("residential-building-cache");

builder.Services.AddSingleton<ResidentialBuildingGenerator>();
builder.Services.AddSingleton<IResidentialBuildingService,  ResidentialBuildingService>();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddSingleton<IProducerService, SnsPublisherService>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();