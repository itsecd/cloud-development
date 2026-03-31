using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var apiPorts = builder.Configuration.GetSection("ApiService:Ports").Get<int[]>()
              ?? throw new InvalidOperationException("ApiService:Ports is not configured.");

var gatewayPort = builder.Configuration.GetValue<int?>("Gateway:Port")
                 ?? throw new InvalidOperationException("Gateway:Port is not configured.");

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var gateway = builder.AddProject<Projects.Vehicle_Gateway>("vehicle-gateway")
    .WithHttpsEndpoint(port: gatewayPort, name: "vehicle-gateway-lb")
    .WaitFor(redis);

for (var i = 0; i < apiPorts.Length; i++)
{
    var httpsPort = apiPorts[i];
    var instanceName = $"vehicle-api-{i + 1}";
    var api = builder.AddProject<Projects.Vehicle_Api>($"vehicle-api-{i + 1}", launchProfileName: null)
        .WithReference(redis)
        .WithHttpsEndpoint(port: httpsPort, name: instanceName)
        .WithEnvironment("INSTANCE_ID", instanceName)
        .WaitFor(redis);

    gateway.WaitFor(api);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();