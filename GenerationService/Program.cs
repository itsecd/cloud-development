using GenerationService.Services;
using Serilog;
using Serilog.Formatting.Compact;
using GenerationService.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new CompactJsonFormatter()));

builder.AddRedisDistributedCache("redis");

builder.Services.AddSingleton<ContractGeneratorService>();
builder.Services.AddSingleton<ContractCacheService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7282",
                "http://localhost:5219")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection("CacheOptions"));

var app = builder.Build();
var instanceId = Guid.NewGuid().ToString()[..8];

app.UseCors("AllowClient");
app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/contracts/{id:int}", async (
    int id,
    ContractCacheService cacheService,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "Request handled by instance {InstanceId}",
        instanceId);

    var contract = await cacheService.GetOrCreateAsync(id);

    return Results.Ok(contract);
});

app.MapGet("/contracts", (ContractGeneratorService generator) =>
{
    var contract = generator.Generate(Random.Shared.Next(1, 100000));
    return Results.Ok(contract);
});

app.MapDefaultEndpoints();

app.Run();