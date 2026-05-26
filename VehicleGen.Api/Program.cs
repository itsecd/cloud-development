using VehicleGen.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IVehicleGenerator, VehicleGenerator>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

builder.AddRedisDistributedCache("cache");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();

app.MapGet("/api/vehicle", async (int id, IVehicleGenerator generator, ICacheService cache, IConfiguration config) =>
{
    if (id <= 0)
        return Results.BadRequest("ID должен быть больше нуля");

    var cached = await cache.RetrieveVehicleAsync(id);
    if (cached is not null)
        return Results.Ok(cached);

    var newVehicle = generator.CreateVehicle(id);
    var ttl = config.GetValue<int>("Cache:ExpirationMinutes", 5);
    await cache.StoreVehicleAsync(newVehicle, ttl);

    return Results.Ok(newVehicle);
});

app.Run();