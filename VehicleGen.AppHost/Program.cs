using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var api = builder.AddProject("vehicle-api", @"..\VehicleGen.Api\VehicleGen.Api.csproj")
    .WithReference(redis);

builder.AddProject("client-wasm", @"..\Client.Wasm\Client.Wasm.csproj")
    .WithReference(api);

builder.Build().Run();