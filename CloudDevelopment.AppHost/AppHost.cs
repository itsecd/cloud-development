var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TransportApp_Api>("transportapp-api");

builder.Build().Run();
