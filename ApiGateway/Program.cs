using ApiGateway.LoadBalancing;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer<WeightedRoundRobinBalancer>((_, _, provider) => new(provider.GetAsync));

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.AllowAnyOrigin();
    policy.WithMethods("GET");
    policy.WithHeaders("Content-Type");
}));

var app = builder.Build();

app.UseCors();

await app.UseOcelot();

app.Run();
