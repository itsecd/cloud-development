using Vehicle.Api.Cache;
using Vehicle.Api.Generation;
using Vehicle.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .WithMethods("GET")
            .WithHeaders("Content-Type");
    });
});

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

app.UseCors("ClientCors");

app.MapControllers();

app.Run();