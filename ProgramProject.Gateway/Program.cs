using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;
using ProgramProject.Gateway.LoadBalancers;

var builder = WebApplication.CreateBuilder(args);

// Добавляем конфигурацию с Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Регистрируем фабрику балансировщика
builder.Services.AddSingleton<ILoadBalancerFactory, QueryBasedLoadBalancerFactory>();

// Добавляем сервисы Ocelot 
builder.Services.AddOcelot()
    .AddKubernetes(); // Провайдер Kubernetes, с которым у меня наконец-то всё запустилось и заработало!

// Добавляем Service Discovery 
builder.Services.AddServiceDiscovery();

var app = builder.Build();

// Обрабатываем все входящие запросы с помощью Ocelot
await app.UseOcelot();

app.Run();