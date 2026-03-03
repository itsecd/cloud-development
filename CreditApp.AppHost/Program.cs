var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var api = builder.AddProject<Projects.CreditApp_Api>("creditapp-api")
    .WithReference(redis)
    .WithReplicas(3)
    .WaitFor(redis);

var gateway = builder.AddProject<Projects.CreditApp_ApiGateway>("creditapp-apigateway")
    .WithReference(api);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
