using Service.Api.Generator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<IEmployeeGeneratorService, EmployeeGeneratorService>();


var app = builder.Build();
app.MapDefaultEndpoints();
app.MapGet("/employee", (IEmployeeGeneratorService service, int id) => service.ProcessEmployee(id));

app.Run();
