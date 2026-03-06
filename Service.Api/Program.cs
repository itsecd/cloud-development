using CloudDevelopment.ServiceDefaults;
using Service.Api.Generator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");
builder.Services.AddScoped<IGeneratorService, GeneratorService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => 
{
    policy.AllowAnyOrigin();
    policy.AllowAnyMethod();
    policy.AllowAnyHeader();
}));
var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/study-course", (IGeneratorService service, int id) => service.ProcessCourse(id));
app.UseCors();
app.Run();
