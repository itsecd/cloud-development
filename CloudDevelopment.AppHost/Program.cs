var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

var courseGeneratorApi = builder.AddProject("course-generator-api", "../CourseGenerator.Api/CourseGenerator.Api.csproj")
    .WithHttpEndpoint(name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

builder.AddProject("client-wasm", "../Client.Wasm/Client.Wasm.csproj")
    .WithReference(courseGeneratorApi)
    .WaitFor(courseGeneratorApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
