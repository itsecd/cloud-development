var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis("cache");

builder.AddProject<Projects.Asp>("back")
    .WithReference(redis);

builder.AddProject<Projects.Client_Wasm>("front");
builder.Build().Run();
