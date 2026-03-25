var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var gateway = builder.AddProject<Projects.VehicleApp_Gateway>("vehicleapp-gateway");

for (var i = 0; i < 7; i++)
{
    var service = builder.AddProject<Projects.VehicleApp_Api>($"vehicleapp-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(5000 + i)
        .WithReference(redis)
        .WaitFor(redis);
    gateway.WaitFor(service);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(gateway);

builder.Build().Run();
