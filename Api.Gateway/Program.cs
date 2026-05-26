using Api.Gateway.LoadBalancer;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer<WeightedRoundRobin>((_, _, provider) => new(provider.GetAsync));

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => origin.StartsWith("https://localhost") || origin.StartsWith("http://localhost"))
            .WithHeaders("Content-Type")
            .WithMethods("GET");
    });
});

var app = builder.Build();

app.UseCors("LocalPolicy");

await app.UseOcelot();

app.Run();