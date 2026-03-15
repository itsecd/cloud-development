var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

builder.AddProject<Asp>("api")
       .WithReference(cache);


builder.Build().Run();
