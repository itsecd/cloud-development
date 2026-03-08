var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("RedisCache").WithRedisInsight(containerName: "insight");

var api0 = builder.AddProject<Projects.AspireApp_ApiService>("warehouse-api-0")
    .WithReference(cache)
    .WithEnvironment("REPLICA_ID", "0")
    .WaitFor(cache);

var api1 = builder.AddProject<Projects.AspireApp_ApiService>("warehouse-api-1")
    .WithReference(cache)
    .WithEnvironment("REPLICA_ID", "1")
    .WaitFor(cache);

var api2 = builder.AddProject<Projects.AspireApp_ApiService>("warehouse-api-2")
    .WithReference(cache)
    .WithEnvironment("REPLICA_ID", "2")
    .WaitFor(cache);

var gateway = builder.AddProject("api-gateway", "../AspireApp.ApiGateway/AspireApp.ApiGateway.csproj")
    .WithReference(api0)
    .WithReference(api1)
    .WithReference(api2)
    .WaitFor(api0).WaitFor(api1).WaitFor(api2);

builder.AddProject("client-wasm", "../Client.Wasm/Client.Wasm.csproj")
    .WithReference(gateway)
    .WithEnvironment("BaseAddress", gateway.GetEndpoint("http"))
    .WaitFor(gateway);

builder.Build().Run();