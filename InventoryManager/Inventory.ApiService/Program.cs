using Inventory.ApiService.Cache;
using Inventory.ApiService.Generation;
using Inventory.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Cache
builder.AddRedisDistributedCache("cache");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI
builder.Services.AddSingleton<Generator>();
builder.Services.AddScoped<IInventoryCache, InventoryCache>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();