using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache");

var api = builder.AddProject<Projects.CreditApp_Api>("creditapp-api")
    .WithReference(redis)
    .WaitFor(redis);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
