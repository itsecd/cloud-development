using ApiGateway.LoadBalancing;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

for (var i = 0; i < 3; i++)
{
    var url = builder.Configuration[$"services__generator-service-{i}__http__0"];
    if (url is null) break;
    var uri = new Uri(url);
    builder.Configuration[$"Routes:0:DownstreamHostAndPorts:{i}:Host"] = uri.Host;
    builder.Configuration[$"Routes:0:DownstreamHostAndPorts:{i}:Port"] = uri.Port.ToString();
}

builder.Services.AddOcelot()
    .AddCustomLoadBalancer<WeightedRoundRobinBalancer>((_, _, provider) => new(provider.GetAsync));

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"]
    ?? throw new InvalidOperationException("Cors:AllowedOrigin is not configured");

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins(allowedOrigin);
    policy.WithMethods("GET");
    policy.WithHeaders("Content-Type");
}));

var app = builder.Build();

app.UseCors();

await app.UseOcelot();

app.Run();
