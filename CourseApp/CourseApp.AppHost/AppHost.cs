var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var apiGateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var courseApi = builder.AddProject<Projects.CourseApp_Api>($"courseapp-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 5213 + i)
        .WithReference(redis)
        .WaitFor(redis);

    apiGateway.WaitFor(courseApi);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(apiGateway);

builder.Build().Run();
