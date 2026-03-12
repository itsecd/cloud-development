using EmployeeApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddScoped<IEmployeeService, EmployeeService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/employees", async (IEmployeeService service, int id) =>
    await service.GetEmployeeById(id));

app.Run();
