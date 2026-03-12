var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander(containerName: "redis-commander");

var api1 = builder.AddProject<Projects.CreditApp_Api>("api1")
    .WithEndpoint("https", e =>
    {
        e.Port = 7401;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithReference(redis)
    .WaitFor(redis);

var api2 = builder.AddProject<Projects.CreditApp_Api>("api2")
    .WithEndpoint("https", e =>
    {
        e.Port = 7402;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithReference(redis)
    .WaitFor(redis);

var api3 = builder.AddProject<Projects.CreditApp_Api>("api3")
    .WithEndpoint("https", e =>
    {
        e.Port = 7403;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithReference(redis)
    .WaitFor(redis);

var api4 = builder.AddProject<Projects.CreditApp_Api>("api4")
    .WithEndpoint("https", e =>
    {
        e.Port = 7404;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithReference(redis)
    .WaitFor(redis);

var api5 = builder.AddProject<Projects.CreditApp_Api>("api5")
    .WithEndpoint("https", e =>
    {
        e.Port = 7405;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithReference(redis)
    .WaitFor(redis);

var gateway = builder.AddProject<Projects.CreditApp_Gateway>("gateway")
    .WithEndpoint("https", e =>
    {
        e.Port = 9001;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithReference(api1)
    .WithReference(api2)
    .WithReference(api3)
    .WithReference(api4)
    .WithReference(api5)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();