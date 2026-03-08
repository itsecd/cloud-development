var builder = DistributedApplication.CreateBuilder(args);

// Redis
var cache = builder.AddRedis("cache")
    .WithRedisCommander();

var apis = new List<IResourceBuilder<ProjectResource>>();


var basePort = 7001;

for (var i = 0; i < 3; i++)
{
    var port = basePort + i;

    var api = builder.AddProject<Projects.Inventory_ApiService>($"apiservice-{i + 1}")
        .WithReference(cache)
        .WaitFor(cache)
        .WithHttpsEndpoint(port: port, name: $"api{i + 1}");

    apis.Add(api);
}

// Gateway
var gateway = builder.AddProject<Projects.Inventory_Gateway>("apigateway")
    .WithHttpsEndpoint(port: 7000, name: "gateway")
    .WaitFor(apis[0])
    .WaitFor(apis[1])
    .WaitFor(apis[2]);

// Client
var client = builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WaitFor(gateway);

builder.Build().Run();