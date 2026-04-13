using System.Text.Json;
using GenerationService.Models;
using GenerationService.Services;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();


// 2. Настраиваем Serilog — структурное логирование
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new CompactJsonFormatter())); // JSON формат в консоль


// 3. Подключаем Redis для кэширования
builder.AddRedisDistributedCache("redis");


// 4. Регистрируем наш генератор как сервис
builder.Services.AddSingleton<ContractGeneratorService>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


// 5. Эндпоинт GET /contracts/{id}
app.MapGet("/contracts/{id}", async (
    string id,                          
    IDistributedCache cache,            
    ContractGeneratorService generator, 
    ILogger<Program> logger) =>         
{
    var cacheKey = $"contract:{id}"; 

    // Пробуем достать из кэша
    var cached = await cache.GetStringAsync(cacheKey);

    if (cached is not null)
    {
        logger.LogInformation("Cache HIT для ключа {CacheKey}", cacheKey);
        var cachedContract = JsonSerializer.Deserialize<SoftwareProjectContract>(cached);
        return Results.Ok(cachedContract);
    }

    
    logger.LogInformation("Cache MISS для ключа {CacheKey}. Генерация нового контракта...", cacheKey);
    var contract = generator.Generate();

    
    var options = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
    await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(contract), options);

    logger.LogInformation("Контракт {ContractId} сохранён в кэш", contract.Id);
    return Results.Ok(contract);
});


app.MapGet("/contracts", (
    ContractGeneratorService generator,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Генерация нового контракта по запросу");
    var contract = generator.Generate();
    return Results.Ok(contract);
});

app.MapDefaultEndpoints();

app.Run();