var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var apiGateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var api = builder.AddProject<Projects.VehicleVault_Api>($"vehiclevault-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(8000 + i)
        .WithReference(cache)
        .WaitFor(cache);
    apiGateway.WaitFor(api);
}
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(apiGateway);

builder.Build().Run();
