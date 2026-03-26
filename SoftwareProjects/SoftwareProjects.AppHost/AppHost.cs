var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight(containerName: "credit-redis-insight");

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var softwareProjectsApi = builder.AddProject<Projects.SoftwareProjects_Api>($"softwareprojects-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(5200 + i)
        .WithReference(cache)
        .WaitFor(cache);
    gateway.WaitFor(softwareProjectsApi);
}
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(gateway);

builder.Build().Run();
