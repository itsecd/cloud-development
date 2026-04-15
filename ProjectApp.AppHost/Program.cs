var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var api0 = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api-0")
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:7173")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.IsProxied = false;
        endpoint.Port = 7173;
        endpoint.TargetPort = 7173;
    })
    .WaitFor(redis);

var api1 = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api-1")
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:7174")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.IsProxied = false;
        endpoint.Port = 7174;
        endpoint.TargetPort = 7174;
    })
    .WaitFor(redis);

var api2 = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api-2")
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:7175")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.IsProxied = false;
        endpoint.Port = 7175;
        endpoint.TargetPort = 7175;
    })
    .WaitFor(redis);

var gateway = builder.AddProject<Projects.ProjectApp_ApiGateway>("projectapp-apigateway")
    .WithReference(api0)
    .WithReference(api1)
    .WithReference(api2);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
