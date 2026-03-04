var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("residential-building-cache")
    .WithRedisInsight(containerName: "residential-building-insight");

var generator1 = builder.AddProject<Projects.ResidentialBuilding_Generator>("generator-1")
    .WithReference(cache, "residential-building-cache")
    .WithEndpoint("http", endpoint => endpoint.Port = 5201)
    .WaitFor(cache);

var generator2 = builder.AddProject<Projects.ResidentialBuilding_Generator>("generator-2")
    .WithReference(cache, "residential-building-cache")
    .WithEndpoint("http", endpoint => endpoint.Port = 5202)
    .WaitFor(cache);

var generator3 = builder.AddProject<Projects.ResidentialBuilding_Generator>("generator-3")
    .WithReference(cache, "residential-building-cache")
    .WithEndpoint("http", endpoint => endpoint.Port = 5203)
    .WaitFor(cache);

var generator4 = builder.AddProject<Projects.ResidentialBuilding_Generator>("generator-4")
    .WithReference(cache, "residential-building-cache")
    .WithEndpoint("http", endpoint => endpoint.Port = 5204)
    .WaitFor(cache);

var generator5 = builder.AddProject<Projects.ResidentialBuilding_Generator>("generator-5")
    .WithReference(cache, "residential-building-cache")
    .WithEndpoint("http", endpoint => endpoint.Port = 5205)
    .WaitFor(cache);

var gateway = builder.AddProject<Projects.ResidentialBuilding_Gateway>("gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 5300)
    .WithReference(generator1)
    .WithReference(generator2)
    .WithReference(generator3)
    .WithReference(generator4)
    .WithReference(generator5)
    .WithExternalHttpEndpoints()
    .WaitFor(generator1)
    .WaitFor(generator2)
    .WaitFor(generator3)
    .WaitFor(generator4)
    .WaitFor(generator5);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();