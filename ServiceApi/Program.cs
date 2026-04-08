using Service.Api.Generator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<IEmployeeGeneratorService, EmployeeGeneratorService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyMethod();
    policy.AllowAnyHeader();
}));

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapGet("/employee", (IEmployeeGeneratorService service, int id) => service.ProcessEmployee(id));
app.UseCors();
app.Run();
