using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;
using ProgramProject.Gateway.LoadBalancers;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.Configure<HttpClientHandler>(options =>
{
    options.AllowAutoRedirect = false;
});

builder.Services.AddOcelot()
    .AddKubernetes()
    .AddCustomLoadBalancer((sp, route, discoveryProvider) =>
    {
        var logger = sp.GetRequiredService<ILogger<QueryBasedLoadBalancer>>();
        return new QueryBasedLoadBalancer(async () => (await discoveryProvider.GetAsync()).ToList(), logger,
            route.LoadBalancerOptions?.Key ?? "id");
    });

builder.Services.AddServiceDiscovery();

var app = builder.Build();

await app.UseOcelot();
app.Run();