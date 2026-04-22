using WarehouseItem.Generator.Generator;
using WarehouseItem.Generator.Service;
using WarehouseItem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("warehouse-item-cache");


builder.Services.AddSingleton<WarehouseItemGenerator>();
builder.Services.AddSingleton<IWarehouseItemCache, WarehouseItemCache>();
builder.Services.AddSingleton<IWarehouseItemService, WarehouseItemService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
