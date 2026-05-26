var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var objectStorage = builder.AddContainer("object-storage", "minio/minio", "latest")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "api", scheme: "http", isProxied: false)
    .WithEndpoint(port: 9001, targetPort: 9001, name: "console", scheme: "http", isProxied: false);

var ports = new[] { 7173, 7174, 7175 };

var gateway = builder.AddProject<Projects.ProjectApp_ApiGateway>("projectapp-apigateway", launchProfileName: null)
    .WithEnvironment("ASPNETCORE_URLS", "https://localhost:7139;http://localhost:5139")
    .WithEndpoint("https", endpoint =>
    {
        endpoint.UriScheme = "https";
        endpoint.IsProxied = false;
        endpoint.Port = 7139;
        endpoint.TargetPort = 7139;
    })
    .WithEndpoint("http", endpoint =>
    {
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
        endpoint.Port = 5139;
        endpoint.TargetPort = 5139;
    });

for (var i = 0; i < ports.Length; i++)
{
    var api = builder.AddProject<Projects.ProjectApp_Api>($"projectapp-api-{i}", launchProfileName: null)
        .WithReference(redis)
        .WithReference(messaging)
        .WithEnvironment("ASPNETCORE_URLS", $"http://localhost:{ports[i].ToString()}")
        .WithEndpoint("http", endpoint =>
        {
            endpoint.UriScheme = "http";
            endpoint.IsProxied = false;
            endpoint.Port = ports[i];
            endpoint.TargetPort = ports[i];
        })
        .WaitFor(messaging)
        .WaitFor(redis);

    gateway.WithReference(api).WaitFor(api);
}

var fileService = builder.AddProject<Projects.ProjectApp_FileService>("projectapp-fileservice", launchProfileName: null)
    .WithReference(messaging)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5180")
    .WithEnvironment("ObjectStorage__Endpoint", "localhost:9000")
    .WithEnvironment("ObjectStorage__AccessKey", "minioadmin")
    .WithEnvironment("ObjectStorage__SecretKey", "minioadmin")
    .WithEnvironment("ObjectStorage__BucketName", "generated-data")
    .WithEnvironment("ObjectStorage__UseSsl", "false")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
        endpoint.Port = 5180;
        endpoint.TargetPort = 5180;
    })
    .WaitFor(messaging)
    .WaitFor(objectStorage);

gateway.WithReference(fileService).WaitFor(fileService);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithHttpEndpoint(port: 5127, name: "client")
    .WaitFor(gateway);

builder.Build().Run();
