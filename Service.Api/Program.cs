using CloudDevelopment.ServiceDefaults;
using Service.Api.Generator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");
builder.Services.AddScoped<IGeneratorService, GeneratorService>();
var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/api/study-course", (IGeneratorService service, int id) => service.ProcessCourse(id));
app.Run();
