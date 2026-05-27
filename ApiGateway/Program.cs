using ApiGateway.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new CompactJsonFormatter()));

// Читаем базовый ocelot.json
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Патчим адреса из Aspire service discovery env-переменных
var gen1 = Environment.GetEnvironmentVariable("services__generation-1__https__0")
           ?? builder.Configuration["services__generation-1__https__0"]
           ?? "https://localhost:7130";
var gen2 = Environment.GetEnvironmentVariable("services__generation-2__https__0")
           ?? builder.Configuration["services__generation-2__https__0"]
           ?? "https://localhost:7131";
var gen3 = Environment.GetEnvironmentVariable("services__generation-3__https__0")
           ?? builder.Configuration["services__generation-3__https__0"]
           ?? "https://localhost:7132";
var fileSvc = Environment.GetEnvironmentVariable("services__file-service__http__0")
              ?? builder.Configuration["services__file-service__http__0"]
              ?? "http://localhost:5300";

Console.WriteLine($"[DEBUG] gen1={gen1}");
Console.WriteLine($"[DEBUG] gen2={gen2}");
Console.WriteLine($"[DEBUG] gen3={gen3}");
Console.WriteLine($"[DEBUG] fileSvc={fileSvc}");

Console.WriteLine("[DEBUG] All relevant env vars:");
foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
{
    var key = e.Key?.ToString() ?? "";
    if (key.Contains("generation") || key.Contains("file-service") || key.Contains("services__"))
        Console.WriteLine($"  {key}={e.Value}");
}

Uri g1 = new(gen1), g2 = new(gen2), g3 = new(gen3), fs = new(fileSvc);

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Routes:0:DownstreamHostAndPorts:0:Host"] = g1.Host,
    ["Routes:0:DownstreamHostAndPorts:0:Port"] = g1.Port.ToString(),
    ["Routes:0:DownstreamHostAndPorts:1:Host"] = g2.Host,
    ["Routes:0:DownstreamHostAndPorts:1:Port"] = g2.Port.ToString(),
    ["Routes:0:DownstreamHostAndPorts:2:Host"] = g3.Host,
    ["Routes:0:DownstreamHostAndPorts:2:Port"] = g3.Port.ToString(),
    ["Routes:1:DownstreamHostAndPorts:0:Host"] = fs.Host,
    ["Routes:1:DownstreamHostAndPorts:0:Port"] = fs.Port.ToString(),
});

builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSingleton<ILoadBalancerCreator, WeightedRoundRobinCreator>();

var app = builder.Build();

app.MapGet("/debug-env", (IConfiguration config) =>
{
    var keys = new[]
    {
        "services__generation-1__https__0",
        "services__generation-2__https__0",
        "services__generation-3__https__0",
        "services__file-service__http__0",
        "services__generation-1__http__0",
        "services__file-service__https__0",
    };
    return keys.ToDictionary(k => k, k => config[k] ?? "(null)");
});

await app.UseOcelot(new OcelotPipelineConfiguration
{
    PreErrorResponderMiddleware = async (ctx, next) =>
    {
        ctx.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        ctx.Response.Headers.Append("Access-Control-Allow-Headers", "*");

        if (ctx.Request.Method == "OPTIONS")
        {
            ctx.Response.StatusCode = 204;
            return;
        }

        await next.Invoke();
    }
});

app.Run();