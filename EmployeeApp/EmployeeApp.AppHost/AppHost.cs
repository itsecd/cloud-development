var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var apiGateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.EmployeeApp_Api>($"employee-app-{i}", launchProfileName: null)
        .WithReference(cache)
        .WaitFor(cache)
        .WithHttpsEndpoint(port: 5100 + i, name: $"employee-https-{i}");
    apiGateway.WaitFor(service);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(apiGateway);

builder.Build().Run();
