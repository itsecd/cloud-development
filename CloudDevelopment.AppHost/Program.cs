var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

builder.AddProject("course-generator-api", "../CourseGenerator.Api/CourseGenerator.Api.csproj")
    .WithHttpEndpoint(name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

builder.Build().Run();
