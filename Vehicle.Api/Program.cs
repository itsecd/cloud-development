using Vehicle.Api.Cache;
using Vehicle.Api.Generation;
using Vehicle.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<VehicleGenerator>();
builder.Services.AddScoped<IVehicleCache, RedisVehicleCache>();
builder.Services.AddScoped<VehicleService>();

builder.AddRedisDistributedCache("redis");

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () =>
{
    var instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "vehicle-api-unknown";

    return Results.Ok(new
    {
        service = "Vehicle.Api",
        status = "ok",
        instanceId,
        message = "Vehicle API is running"
    });
});

app.MapControllers();

app.Use(async (context, next) =>
{
    var instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "vehicle-api-unknown";
    context.Response.Headers["X-Instance-Id"] = instanceId;
    await next();
});

app.Run();