var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TrainingCourse_Api>("trainingcourse-api");

builder.Build().Run();
