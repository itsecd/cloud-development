using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Service.Gateway.Balancer;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Configuration.AddJsonFile("apiGateway.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration).AddCustomLoadBalancer((serviceProvider, route, discoveryProvider) =>
{
    return new QueryBasedLoadBalancer(discoveryProvider.GetAsync);
});

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

app.UseCors();

app.UseHttpsRedirection();
await app.UseOcelot();
app.Run();
