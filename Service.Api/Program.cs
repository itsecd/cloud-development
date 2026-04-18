using Bogus;
using Service.Api.Entity;
using Service.Api.Generator;
using Service.Api.Redis;
using Service.Api.Services;
using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using StackExchange.Redis;
using Service.Api.Broker;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<Faker<ProgramProject>, ProgramProjectFaker>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("RedisCache");
    if (configuration != null) return ConnectionMultiplexer.Connect(configuration);
    else throw new InvalidOperationException("u should fix the redis connection");
});

builder.Services.AddScoped<RedisService>();
builder.Services.AddScoped<ProgramProjectCacheService>();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddScoped<IProducerService, SnsPublisherService>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

var app = builder.Build();

app.MapHealthChecks("health");

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.MapGet("/program-proj", async (int id, ProgramProjectCacheService cacheService) =>
{
    return Results.Ok(await cacheService.GetOrGenerateAsync(id));
})
.WithName("GetProgramProject");
app.MapGet("/", () => Results.Ok("ok"));
app.Run();
