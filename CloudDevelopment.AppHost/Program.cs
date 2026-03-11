var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

builder.AddProject("course-generator-api", "../CourseGenerator.Api/CourseGenerator.Api.csproj")
    .WithReference(redis);

builder.Build().Run();
