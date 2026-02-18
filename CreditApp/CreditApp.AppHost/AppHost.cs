var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("credit-cache")
                   .WithRedisInsight(containerName: "credit-redis-insight");

var api = builder.AddProject<Projects.CreditApp_Api>("credit-api")
       .WithReference(redis)
       .WaitFor(redis);

builder.AddProject<Projects.Client_Wasm>("client")
        .WithReference(api)
        .WaitFor(api);

builder.Build().Run();

