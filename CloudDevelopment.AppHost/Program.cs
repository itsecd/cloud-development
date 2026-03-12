var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

var courseGeneratorApi = builder.AddProject<Projects.CourseGenerator_Api>("course-generator-api")
    .WithHttpEndpoint(name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(courseGeneratorApi)
    .WaitFor(courseGeneratorApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
