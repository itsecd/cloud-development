var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var api = builder.AddProject<Projects.TrainingCourse_Api>("training-course-api")
    .WithReference(redis)
    .WaitFor(redis);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();