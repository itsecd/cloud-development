var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var api = builder.AddProject<Projects.CourseApp_Api>($"courseapp-api-{i}", launchProfileName: null)
        .WithReference(redis)
        .WaitFor(redis)
        .WithHttpsEndpoint(port: 8000 + i);
    gateway.WaitFor(api);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();