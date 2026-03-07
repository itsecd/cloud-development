using CourseManagement.ApiGateway.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Подключение конфига Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Добавление Ocelot с Query Based балансировщиком нагрузки
builder.Services.AddOcelot()
    .AddCustomLoadBalancer<QueryLoadBalancer>((serviceProvider, downstreamRoute, serviceDiscoveryProvider) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<QueryLoadBalancer>>();

        var services = serviceDiscoveryProvider.GetAsync().GetAwaiter().GetResult().ToList();

        return new QueryLoadBalancer(logger, services);
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.AllowAnyOrigin()
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

var app = builder.Build();

app.UseCors("AllowClient");

await app.UseOcelot();

app.Run();