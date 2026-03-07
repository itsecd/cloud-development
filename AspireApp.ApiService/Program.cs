using AspireApp.ApiService.Generator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<IWarehouseCache, WarehouseCache>();
builder.Services.AddSingleton<WarehouseGenerator>(); 
builder.Services.AddScoped<IWarehouseGeneratorService, WarehouseGeneratorService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5127")
              .WithMethods("GET")                
              .WithHeaders("Content-Type"); 
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/warehouse", async (IWarehouseGeneratorService service, int id) =>
{
    var warehouse = await service.ProcessWarehouse(id);
    return Results.Ok(warehouse);
});

app.UseCors();


app.Run();
