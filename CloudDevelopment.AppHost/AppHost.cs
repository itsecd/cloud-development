using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var ports = builder.Configuration.GetSection("ApiService:Ports").Get<int[]>()
           ?? throw new InvalidOperationException("ApiService:Ports is not  configured.");

var cache = builder.AddRedis("credit-order-cache")
    .WithRedisInsight(containerName: "credit-order-insight");

var gateway = builder.AddProject<Projects.CreditOrder_Gateway>("gateway");
for (var i = 0; i < ports.Length; i++)
{
    var httpsPort = ports[i];
    var httpPort = ports[i] - 1000;
    var urls = $"https://localhost:{httpsPort};http://localhost:{httpPort}";

    var geenrator = builder.AddProject<Projects.Generator>($"generator-r{i + 1}")
            .WithReference(cache, "RedisCache")
            .WithEnvironment("ASPNETCORE_URLS", urls)
            .WaitFor(cache);

    gateway.WaitFor(geenrator);
}

var client = builder.AddProject<Projects.Client_Wasm>("credit-order-wasm")
    .WaitFor(gateway);

builder.Build().Run();
