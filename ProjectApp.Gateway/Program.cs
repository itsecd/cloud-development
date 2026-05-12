using System.Text.Json.Nodes;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;
using ProjectApp.Gateway.LoadBalancer;
using ProjectApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

try
{
    var env = Environment.GetEnvironmentVariable("DOWNSTREAM_HOSTS");
    if (!string.IsNullOrWhiteSpace(env))
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "ocelot.json");
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            var root = JsonNode.Parse(json);
            var routes = root?["Routes"] as JsonArray;
            if (routes is not null)
            {
                var entries = env.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var downstreamArray = new JsonArray();

                foreach (var entry in entries)
                {
                    var parts = entry.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length < 2 || !int.TryParse(parts[1], out var port))
                    {
                        continue;
                    }

                    var weight = 1;
                    if (parts.Length >= 3)
                    {
                        int.TryParse(parts[2], out weight);
                    }

                    downstreamArray.Add(new JsonObject
                    {
                        ["Host"] = parts[0],
                        ["Port"] = port,
                        ["Metadata"] = new JsonObject
                        {
                            ["weight"] = weight.ToString()
                        }
                    });
                }

                if (downstreamArray.Count > 0)
                {
                    foreach (var route in routes)
                    {
                        var lb = route?["LoadBalancerOptions"] as JsonObject;
                        if (lb is not null &&
                            string.Equals(lb["Type"]?.ToString(), "WeightedRoundRobin", StringComparison.OrdinalIgnoreCase))
                        {
                            route!["DownstreamHostAndPorts"] = downstreamArray;
                        }
                    }

                    File.WriteAllText(configPath, root!.ToJsonString(new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
                }
            }
        }
    }
}
catch
{
}

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [])
            .WithMethods("GET")
            .AllowAnyHeader();
    });
});

builder.Services.AddOcelot();
builder.Services.AddSingleton<ILoadBalancerCreator, WeightedRoundRobinCreator>();

var app = builder.Build();

app.UseCors("AllowClient");
app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();
