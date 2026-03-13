using Microsoft.Extensions.Configuration;

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

    var generator = builder.AddProject<Projects.Generator>($"generator-r{i + 1}", launchProfileName: null)
            .WithReference(cache, "RedisCache")
            .WithHttpEndpoint(httpPort)
            .WithHttpsEndpoint(httpsPort)
            .WaitFor(cache);

    gateway.WaitFor(generator);
}

var client = builder.AddProject<Projects.Client_Wasm>("credit-order-wasm")
    .WaitFor(gateway);

builder.Build().Run();
