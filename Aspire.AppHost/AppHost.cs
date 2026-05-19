var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache").WithRedisInsight();

var localstack = builder.AddContainer("localstack", "localstack/localstack", "3.8")
    .WithEnvironment("SERVICES", "s3,sns,sqs")
    .WithEnvironment("DEFAULT_REGION", "us-east-1")
    .WithHttpEndpoint(targetPort: 4566, name: "localstack");

var fileService = builder.AddProject<Projects.FileService>("file-service")
    .WithEnvironment("AWS__ServiceURL", localstack.GetEndpoint("localstack"))
    .WithEnvironment("AWS__Region", "us-east-1")
    .WithEnvironment("AWS__TopicName", "vehicle-contracts")
    .WithEnvironment("AWS__BucketName", "vehicle-files")
    .WaitFor(localstack);

fileService.WithEnvironment("FileService__SnsCallbackUrl", fileService.GetEndpoint("http"));

var gateway = builder.AddProject<Projects.ApiGateway>("gateway")
    .WithEndpoint("https", endpoint => endpoint.Port = 8095);

for (var i = 0; i < 3; i++)
{
    var generator = builder.AddProject<Projects.Server>($"back-{i}")
        .WithEndpoint("https", endpoint => endpoint.Port = 8090 + i)
        .WithReference(redis)
        .WithEnvironment("AWS__ServiceURL", localstack.GetEndpoint("localstack"))
        .WithEnvironment("AWS__Region", "us-east-1")
        .WithEnvironment("AWS__TopicName", "vehicle-contracts")
        .WaitFor(redis)
        .WaitFor(localstack)
        .WaitFor(fileService);

    gateway.WithReference(generator)
        .WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("front")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
