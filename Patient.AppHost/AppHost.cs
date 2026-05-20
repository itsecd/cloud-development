var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("patient-cache")
    .WithRedisInsight(containerName: "patient-insight");

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var generator = builder.AddProject<Projects.Patient_Generator>($"generator-{i}", launchProfileName: null)
        .WithHttpsEndpoint(5200 + i)
        .WithReference(cache, "patient-cache")
        .WaitFor(cache);
    gateway.WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
