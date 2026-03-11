var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

builder.AddProject<Projects.CourseGenerator_Api>("course-generator-api")
    .WithReference(redis)
    .WaitFor(redis);

builder.Build().Run();
