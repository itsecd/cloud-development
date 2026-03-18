var builder = DistributedApplication.CreateBuilder(args);

var weights = builder.Configuration
    .GetSection("ReplicaWeights")
    .GetChildren()
    .Select(x => double.Parse(x.Value!, System.Globalization.CultureInfo.InvariantCulture))
    .ToArray();

if (weights.Length == 0)
    weights = [0.5, 0.3, 0.2];

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var replica1 = builder.AddProject<Projects.VehicleApi>("vehicleapi-1")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5101");

var replica2 = builder.AddProject<Projects.VehicleApi>("vehicleapi-2")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5102");

var replica3 = builder.AddProject<Projects.VehicleApi>("vehicleapi-3")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5103");

var gateway = builder.AddProject<Projects.ApiGateway>("apigateway")
    .WithReference(replica1)
    .WithReference(replica2)
    .WithReference(replica3)
    .WaitFor(replica1)
    .WaitFor(replica2)
    .WaitFor(replica3)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5200")
    .WithEnvironment("WeightedRandom__vehicles__Weights__0", weights[0].ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
    .WithEnvironment("WeightedRandom__vehicles__Weights__1", weights[1].ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
    .WithEnvironment("WeightedRandom__vehicles__Weights__2", weights[2].ToString("F2", System.Globalization.CultureInfo.InvariantCulture));

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();