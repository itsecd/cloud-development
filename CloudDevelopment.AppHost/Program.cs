var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

builder.AddProject("course-generator-api", "../CourseGenerator.Api/CourseGenerator.Api.csproj")
    .WithHttpEndpoint(name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(redis)
    .WithExternalHttpEndpoints();

builder.Build().Run();
