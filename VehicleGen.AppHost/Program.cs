using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var api = builder.AddProject<Projects.VehicleGen_Api>("vehicle-api")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(api)
    .WithEndpoint("http", endpoint => { endpoint.Port = 7201; endpoint.IsProxied = false; })
    .WithEndpoint("https", endpoint => { endpoint.Port = 7202; endpoint.IsProxied = false; })
    .WaitFor(api);

builder.Build().Run();