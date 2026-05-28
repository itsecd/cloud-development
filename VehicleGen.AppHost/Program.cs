var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisCommander();

var localstack = builder.AddLocalStack("localstack", configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.Port = 14566;
});

var gateway = builder.AddProject<Projects.VehicleGen_Gateway>("api-gateway");

var fileService = builder.AddProject<Projects.File_Service>("file-service")
    .WithReference(localstack)
    .WithEnvironment("AWS__Resources__SQSQueueName", "vehicle-queue")
    .WithEnvironment("AWS__Resources__S3BucketName", "vehicle-storage-bucket");

for (var i = 0; i < 5; i++)
{
    var api = builder.AddProject<Projects.VehicleGen_Api>($"vehicle-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(9000 + i)
        .WithReference(cache)
        .WaitFor(cache)
        .WaitFor(fileService)
        .WithEnvironment("AWS__Resources__SQSQueueName", "vehicle-queue")
        .WithEnvironment("AWS__Resources__S3BucketName", "vehicle-storage-bucket");

    gateway.WithReference(api);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(gateway)
    .WithEndpoint("http", endpoint => { endpoint.Port = 7201; endpoint.IsProxied = false; })
    .WithEndpoint("https", endpoint => { endpoint.Port = 7202; endpoint.IsProxied = false; })
    .WaitFor(gateway);

builder.UseLocalStack(localstack);

builder.Build().Run();