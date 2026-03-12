using Api.Gateway.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer<QueryBased>((_, _, discoveryProvider) => new(discoveryProvider.GetAsync));

var clientOrigin = builder.Configuration["ClientOrigin"]
    ?? throw new InvalidOperationException("ClientOrigin is not configured");

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins(clientOrigin);
    policy.WithMethods("GET");
    policy.AllowAnyHeader();
}));

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();
