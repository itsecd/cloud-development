var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisCommander();

var gateway = builder.AddProject<Projects.VehicleGen_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var api = builder.AddProject<Projects.VehicleGen_Api>($"vehicle-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(9000 + i)
        .WithReference(cache);

    gateway.WithReference(api);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(gateway);

builder.Build().Run();