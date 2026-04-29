var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(); // добавляет веб-интерфейс Redis Insight

builder.AddProject<Projects.GenerationService>("generation-service")
    .WithReference(redis)      
    .WaitFor(redis);

builder.AddProject<Projects.Client_Wasm>("client-wasm");

builder.Build().Run();