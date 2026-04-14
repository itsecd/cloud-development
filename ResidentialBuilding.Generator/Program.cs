using Amazon.SimpleNotificationService;
using Generator.Generator;
using Generator.Service;
using Generator.Service.Cache;
using Generator.Service.Messaging;
using LocalStack.Client.Extensions;
using ResidentialBuilding.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("residential-building-cache");

builder.Services.AddSingleton<ResidentialBuildingGenerator>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IPublisherService, SnsPublisherService>();
builder.Services.AddSingleton<IResidentialBuildingService,  ResidentialBuildingService>();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();