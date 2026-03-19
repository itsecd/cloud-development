using Domain.Interfaces;
using Infrastructure.Generators;
using Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IVehicleModelGenerator, VehicleModelGenerator>();
builder.Services.AddScoped<IVehicleContractGenerator, VehicleContractGenerator>();

builder.AddRedisDistributedCache("cache");

builder.Services.AddScoped<IVehicleContractCachedService, VehicleContractCachedService>();

var clientAddress = builder.Configuration["ClientAddress"]
    ?? throw new InvalidOperationException("ClientAddress is not configured.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientPolicy", policy =>
    {
        policy
            .WithOrigins(clientAddress) // адрес клиента
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors("ClientPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
