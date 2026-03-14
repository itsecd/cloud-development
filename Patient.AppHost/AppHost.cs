using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("patient-cache")
    .WithRedisInsight(containerName: "patient-insight");

var generator = builder.AddProject("generator", "../Patient.Generator/Patient.Generator.csproj")
    .WithReference(cache, "patient-cache")
    .WaitFor(cache);

builder.AddProject("client", "../Client.Wasm/Client.Wasm.csproj")
    .WaitFor(generator);

builder.Build().Run();
