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

// CORS — разрешаем запросы от клиента
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7282")
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection("CacheOptions"));

var app = builder.Build();

app.UseCors("ClientPolicy");
app.UseSwagger();
app.UseSwaggerUI();

// GET /contracts/{id} — получить контракт по id (с кэшированием)
app.MapGet("/contracts/{id:int}", async (
    int id,
    ContractCacheService cacheService) =>
{
    var contract = await cacheService.GetOrCreateAsync(id);
    return Results.Ok(contract);
});

// GET /contracts — сгенерировать случайный контракт
app.MapGet("/contracts", (ContractGeneratorService generator) =>
{
    var contract = generator.Generate(Random.Shared.Next(1, 100000));
    return Results.Ok(contract);
});

app.MapDefaultEndpoints();

app.Run();