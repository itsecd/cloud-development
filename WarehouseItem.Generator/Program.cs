using WarehouseItem.Generator.Generator;
using WarehouseItem.Generator.Service;
using WarehouseItem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("warehouse-item-cache");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
    {
        policy
            .AllowAnyOrigin()
            .WithHeaders("Content-Type")
            .WithMethods("GET");
    });
});

builder.Services.AddSingleton<WarehouseItemGenerator>();
builder.Services.AddSingleton<IWarehouseItemService, WarehouseItemService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowLocalDev");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
