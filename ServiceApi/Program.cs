using ServiceApi.Generator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<IGeneratorService, GeneratorService>();
builder.Services.AddScoped<IProgramProjectCache, ProgramProjectCache>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins("http://localhost:5127")
          .WithMethods("GET")
          .AllowAnyHeader();
}));


var app = builder.Build();
app.UseCors();
app.MapDefaultEndpoints();

app.MapGet("/program-project", (IGeneratorService service, int id) => service.ProcessProgramProject(id));
app.Run();
