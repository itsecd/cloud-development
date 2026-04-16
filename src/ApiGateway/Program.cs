using ApiGateway.LoadBalancing;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var ocelotConfig = builder.Configuration.GetSection("Routes:0");
var hosts = new List<object>();

for (var i = 1; ; i++)
{
    var url = builder.Configuration[$"services__vehicleapi-{i}__http__0"];
    if (url == null) break;

    var uri = new Uri(url);
    hosts.Add(new { Host = uri.Host, Port = uri.Port });
}

if (hosts.Count > 0)
{
    builder.Configuration["Routes:0:DownstreamHostAndPorts"] = null;
    for (var i = 0; i < hosts.Count; i++)
    {
        var h = (dynamic)hosts[i];
        builder.Configuration[$"Routes:0:DownstreamHostAndPorts:{i}:Host"] = h.Host;
        builder.Configuration[$"Routes:0:DownstreamHostAndPorts:{i}:Port"] = h.Port.ToString();
    }
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var weights = builder.Configuration
    .GetSection("WeightedRandom:Weights")
    .Get<double[]>() ?? [0.5, 0.3, 0.2];

builder.Services.AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer<WeightedRandomLoadBalancer>((serviceProvider, route, serviceDiscoveryProvider) =>
    {
        var services = serviceDiscoveryProvider.GetAsync().GetAwaiter().GetResult().ToList();
        return new WeightedRandomLoadBalancer(services, weights);
    });

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();  

await app.UseOcelot();

app.Run();