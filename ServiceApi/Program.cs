using ServiceApi.Generator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<IGeneratorService, GeneratorService>();
builder.Services.AddScoped<IProgramProjectCache, ProgramProjectCache>();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/program-project", (IGeneratorService service, int id) => service.ProcessProgramProject(id));
app.Run();
