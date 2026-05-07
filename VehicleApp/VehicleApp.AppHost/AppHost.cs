var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var api = builder.AddProject<Projects.VehicleApp_Api>($"vehicleapp-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(5250 + i)
        .WithReference(cache)
        .WaitFor(cache);
    gateway.WithReference(api).WaitFor(api);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(gateway);

builder.Build().Run();
