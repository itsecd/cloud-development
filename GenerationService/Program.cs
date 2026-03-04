using AppHost.ServiceDefaults;
using CachingService.Services;
using GenerationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<IGeneratorService, GeneratorService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyMethod();
    policy.AllowAnyHeader();
}));

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/patient", (IGeneratorService service, int id) => service.GenerateAsync(id));
app.UseCors();

app.Run();
