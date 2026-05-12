var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis").WithRedisInsight();

var api = builder.AddProject<Projects.CompanyEmployee_ApiGateway>("companyemployee-apigateway")
    .WithHttpEndpoint(name: "gateway", port: 5212);

var minio = builder.AddContainer("minio","minio/minio")
    .WithEndpoint(name: "http", port: 9000, targetPort: 9000)
    .WithEndpoint(name: "console", port: 9001, targetPort: 9001)
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithArgs("server", "/data", "--console-address", ":9001");

var rabbitmq = builder.AddRabbitMQ("rabbitmq");

for (var i = 1; i <= 3; i++)
{
    var generator = builder
        .AddProject<Projects.CompanyEmployee_ApiService>($"generator-{i}")
        .WithReference(redis)
        .WithReference(rabbitmq)
        .WaitFor(redis)
        .WaitFor(rabbitmq)
        .WithHttpEndpoint(name: $"http");

    api.WithReference(generator)
        .WaitFor(generator);
}

builder.AddProject<Projects.CompanyEmployee_FileService>("fileservice")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(minio)
    .WaitFor(rabbitmq)
    .WithEnvironment("Minio__Endpoint", "localhost:9000")
    .WithEnvironment("Minio__AccessKey", "minioadmin")
    .WithEnvironment("Minio__SecretKey", "minioadmin")
    .WithEnvironment("Minio__BucketName", "companyemployee");

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
//public partial class Program;