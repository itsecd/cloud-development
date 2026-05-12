var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var gateway = builder.AddProject<Projects.ProjectApp_Gateway>("projectapp-gateway");

var apiPorts = new[] { 5500, 5501, 5502 };
var weights = new[] { 3, 2, 1 };
var downstreamHosts = new List<string>();

for (var i = 0; i < apiPorts.Length; i++)
{
    var service = builder.AddProject<Projects.ProjectApp_Api>($"projectapp-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(apiPorts[i])
        .WithReference(redis)
        .WaitFor(redis);

    gateway.WithReference(service);
    gateway.WaitFor(service);

    downstreamHosts.Add($"localhost:{apiPorts[i]}:{weights[i]}");
}

gateway.WithEnvironment("DOWNSTREAM_HOSTS", string.Join(',', downstreamHosts));

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
