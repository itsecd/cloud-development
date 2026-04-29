using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using SoftwareProjects.Api.Messaging;
using SoftwareProjects.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

builder.Services.AddScoped<IProjectPublisher, SnsProjectPublisher>();
builder.Services.AddScoped<ISoftwareProjectCacheService, SoftwareProjectCacheService>();
builder.Services.AddScoped<ISoftwareProjectService, SoftwareProjectService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/software-projects", async (int id, ISoftwareProjectService service) =>
    Results.Ok(await service.GetById(id)));

app.Run();
