using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using ProgramProject.Gateway.LoadBalancers;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

Console.OutputEncoding = System.Text.Encoding.UTF8;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
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
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddOcelot()
    .AddCustomLoadBalancer((sp, route, discoveryProvider) =>
    {
        var logger = sp.GetRequiredService<ILogger<QueryBasedLoadBalancer>>();
        return new QueryBasedLoadBalancer(async () => (await discoveryProvider.GetAsync()).ToList(), logger,
            route.LoadBalancerOptions?.Key ?? "id");
    });

builder.Services.AddServiceDiscovery();

var app = builder.Build();

app.UseCors("AllowClient");

await app.UseOcelot();
app.Run();