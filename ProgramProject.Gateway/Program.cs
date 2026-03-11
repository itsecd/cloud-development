using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;
using ProgramProject.Gateway.LoadBalancers;

var builder = WebApplication.CreateBuilder(args);

// Добавляем конфигурацию с Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Регистрируем балансировщик
builder.Services.AddOcelot()
    .AddKubernetes()
    .AddCustomLoadBalancer((serviceProvider, route, serviceDiscoveryProvider) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<QueryBasedLoadBalancer>>();
        var services = serviceDiscoveryProvider.GetAsync().GetAwaiter().GetResult().ToList();

        return new QueryBasedLoadBalancer(services, logger);
    });

// Добавляем Service Discovery 
builder.Services.AddServiceDiscovery();

var app = builder.Build();

// Обрабатываем все входящие запросы с помощью Ocelot
await app.UseOcelot();

app.Run();