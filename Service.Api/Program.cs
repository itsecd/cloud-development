using Bogus;
using Service.Api.Entity;
using Service.Api.Generator;
using Service.Api.Redis;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<Faker<ProgramProject>, ProgramProjectFaker>();
builder.Services.AddSingleton<ProgramProjectGeneratorService>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("programproj-cache");
    if (configuration != null) return ConnectionMultiplexer.Connect(configuration);
    else throw new InvalidOperationException("u should fix the redis connection");
});

builder.Services.AddScoped<RedisCacheService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/program-proj", async (int id, ProgramProjectGeneratorService generatorService, RedisCacheService cs) =>
{
    var key = $"project:{id}";
    var programProject = await cs.GetAsync<ProgramProject>(key);
    if(programProject != null) return Results.Ok(programProject);
    var newProject = generatorService.GetProgramProjectInstance(id);
    await cs.SetAsync(key, newProject, TimeSpan.FromHours(12));
    return Results.Ok(newProject);
})
.WithName("GetProgramProject")
.WithOpenApi();

app.Run();
