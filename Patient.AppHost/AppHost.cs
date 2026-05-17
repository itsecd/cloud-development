var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("patient-cache")
    .WithRedisInsight(containerName: "patient-insight");

var generator = builder.AddProject<Projects.Patient_Generator>("generator")
    .WithReference(cache, "patient-cache")
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(generator);

builder.Build().Run();
