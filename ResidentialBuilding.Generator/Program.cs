using Generator.Generator;
using Generator.Service;
using ResidentialBuilding.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("residential-building-cache");

builder.Services.AddSingleton<ResidentialBuildingGenerator>();
builder.Services.AddSingleton<IResidentialBuildingService,  ResidentialBuildingService>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();