using Bogus;
using Service.Api.Entity;
using Service.Api.Generator;
using Service.Api.Redis;
using Service.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<Faker<ProgramProject>, ProgramProjectFaker>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("RedisCache");
    if (configuration != null) return ConnectionMultiplexer.Connect(configuration);
    else throw new InvalidOperationException("u should fix the redis connection");
});

builder.Services.AddScoped<RedisService>();
builder.Services.AddScoped<ProgramProjectCacheService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(origin =>
                {
                    try
                    {
                        var uri = new Uri(origin);
                        return uri.Host == "localhost";
                    }
                    catch
                    {
                        return false;
                    }
                })
              .WithMethods("GET")
              .AllowAnyHeader());
});

var app = builder.Build();

app.MapHealthChecks("health");

app.UseCors();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.MapGet("/program-proj", async (int id, ProgramProjectCacheService cacheService) =>
{
    return Results.Ok(await cacheService.GetOrGenerateAsync(id));
})
.WithName("GetProgramProject");
app.MapGet("/", () => Results.Ok("ok"));
app.Run();
