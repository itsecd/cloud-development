using Api.Gateway.LoadBalancer;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer<WeightedRandom>((sp, _, discoveryProvider) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        return new WeightedRandom(discoveryProvider.GetAsync, configuration);
    });
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins("http://localhost:5127")
          .WithMethods("GET")
          .AllowAnyHeader();
}));

var app = builder.Build();
app.UseCors();
await app.UseOcelot();
app.Run();