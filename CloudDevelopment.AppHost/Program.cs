var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

var courseGeneratorApi = builder.AddProject<Projects.CourseGenerator_Api>("course-generator-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(name: "api", port: 5117);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(courseGeneratorApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
