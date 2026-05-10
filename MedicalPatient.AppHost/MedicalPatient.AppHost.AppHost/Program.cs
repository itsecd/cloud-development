var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var minioAccessKey = builder.Configuration["MinIO:AccessKey"]!;
var minioSecretKey = builder.Configuration["MinIO:SecretKey"]!;

var minio = builder.AddMinioContainer("minio")
    .WithEnvironment("MINIO_ROOT_USER", minioAccessKey)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioSecretKey)
    .WaitFor(redis);

minio.WithEndpoint("http", endpoint => endpoint.Port = 9000);
minio.WithEndpoint("console", endpoint => endpoint.Port = 9001);

var sqs = builder.AddContainer("elasticmq", "softwaremill/elasticmq-native")
    .WithHttpEndpoint(targetPort: 9324, name: "http")
    .WithHttpHealthCheck("/?Action=ListQueues", endpointName: "http");


builder.AddProject<Projects.MedicalPatient_FileService>("medicalpatient-fileservice")
    .WithEnvironment("MinIO__Endpoint", minio.GetEndpoint("http"))
    .WithEnvironment("SQS__ServiceUrl", sqs.GetEndpoint("http"))
    .WaitFor(minio)
    .WaitFor(sqs);

var generator1 = builder.AddProject<Projects.MedicalPatient_Generator>("generator-1")
    .WithEndpoint("http", endpoint => endpoint.Port = 5101)
    .WithReference(redis)
    .WithEnvironment("SQS__ServiceUrl", sqs.GetEndpoint("http"))
    .WithEnvironment("SQS__QueueName", "medical-patients")
    .WaitFor(redis)
    .WaitFor(sqs)
    .WithExternalHttpEndpoints();

var generator2 = builder.AddProject<Projects.MedicalPatient_Generator>("generator-2")
    .WithEndpoint("http", endpoint => endpoint.Port = 5102)
    .WithReference(redis)
    .WithEnvironment("SQS__ServiceUrl", sqs.GetEndpoint("http"))
    .WithEnvironment("SQS__QueueName", "medical-patients")
    .WaitFor(redis)
    .WaitFor(sqs)
    .WithExternalHttpEndpoints();

var generator3 = builder.AddProject<Projects.MedicalPatient_Generator>("generator-3")
    .WithEndpoint("http", endpoint => endpoint.Port = 5103)
    .WithReference(redis)
    .WithEnvironment("SQS__ServiceUrl", sqs.GetEndpoint("http"))
    .WithEnvironment("SQS__QueueName", "medical-patients")
    .WaitFor(redis)
    .WaitFor(sqs)
    .WithExternalHttpEndpoints();

var gateway = builder
    .AddProject<Projects.MedicalPatient_ApiGateway>("medicalpatient-apigateway")
    .WithReference(generator1)
    .WithReference(generator2)
    .WithReference(generator3)
    .WithExternalHttpEndpoints()
    .WaitFor(generator1)
    .WaitFor(generator2)
    .WaitFor(generator3);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.AddProject<Projects.WebApplication1>("webapplication1");

builder.AddProject<Projects.MedicalPatient_Tests>("medicalpatient-tests");

builder.Build().Run();

