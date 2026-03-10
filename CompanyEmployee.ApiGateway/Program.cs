using CompanyEmployee.ApiGateway;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
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

builder.Configuration.AddJsonFile("ocelotSettings.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration).AddCustomLoadBalancer((serviceProvider, _, discoveryProvider) => 
    new QueryBasedLoadBalancer(discoveryProvider.GetAsync, 
        serviceProvider.GetRequiredService<ILogger<QueryBasedLoadBalancer>>()));

var app = builder.Build();

app.UseCors("wasm");

await app.UseOcelot();

await app.RunAsync();