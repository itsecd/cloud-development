using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Service.ApiGateway.balancer;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Configuration.AddJsonFile("apiGateway.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration).AddCustomLoadBalancer((serviceProvider, route, discoveryProvider) =>
{
    return new QueryBasedLoadBalancer(discoveryProvider.GetAsync);
});
var app = builder.Build();

app.UseHttpsRedirection();
await app.UseOcelot();
app.Run();
