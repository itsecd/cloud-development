var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("credit-cache")
                   .WithRedisInsight(containerName: "credit-redis-insight");

var replicaPorts = new Dictionary<string, (int Http, int Https)>
{
    ["credit-api-1"] = (7001, 7081),
    ["credit-api-2"] = (7002, 7082),
    ["credit-api-3"] = (7003, 7083),
};

var apiReplicas = new List<IResourceBuilder<ProjectResource>>();

foreach (var (name, (httpPort, httpsPort)) in replicaPorts)
{
    var replica = builder.AddProject<Projects.CreditApp_Api>(name)
        .WithEndpoint("http", endpoint => endpoint.Port = httpPort)
        .WithEndpoint("https", endpoint => endpoint.Port = httpsPort)
        .WithReference(redis)
        .WaitFor(redis);

    apiReplicas.Add(replica);
}

var gateway = builder.AddProject<Projects.CreditApp_Gateway>("creditapp-gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 7200)
    .WithEndpoint("https", endpoint => endpoint.Port = 7201)
    .WithExternalHttpEndpoints();

foreach (var replica in apiReplicas)
{
    gateway.WithReference(replica)
           .WaitFor(replica);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
