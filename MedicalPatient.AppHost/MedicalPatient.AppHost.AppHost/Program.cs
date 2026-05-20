var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var localstack = builder.AddContainer("localstack", "localstack/localstack:3.0")
    .WithEnvironment("SERVICES", "sqs,s3")
    .WithEnvironment("AWS_ACCESS_KEY_ID", "admin")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "admin")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "http");

var sqsUrl = localstack.GetEndpoint("http");

var fileService = builder.AddProject<Projects.MedicalPatient_FileService>("medicalpatient-fileservice")
    .WithEnvironment("LocalStack__UseLocalStack", "true")
    .WithEnvironment("LocalStack__LocalStackUrl", localstack.GetEndpoint("http"))
    .WithEnvironment("S3__BucketName", "medical-patient")
    .WithEnvironment("SQS__ServiceUrl", sqsUrl)
    .WithEnvironment("SQS__QueueName", "medical-patients")
    .WaitFor(localstack);

var generatorProjects = new List<(string name, int port)>
{
    ("generator-1", 5101),
    ("generator-2", 5102),
    ("generator-3", 5103)
};

var generators = new List<IResourceBuilder<ProjectResource>>();

foreach (var (name, port) in generatorProjects)
{
    var generator = builder.AddProject<Projects.MedicalPatient_Generator>(name)
        .WithEndpoint("http", endpoint => endpoint.Port = port)
        .WithReference(redis)
        .WithEnvironment("SQS__ServiceUrl", sqsUrl)
        .WithEnvironment("SQS__QueueName", "medical-patients")
        .WaitFor(redis)
        .WaitFor(localstack)
        .WithExternalHttpEndpoints();

    generators.Add(generator);
    fileService.WaitFor(generator);
}

var gateway = builder
    .AddProject<Projects.MedicalPatient_ApiGateway>("medicalpatient-apigateway")
    .WithExternalHttpEndpoints();

foreach (var generator in generators)
{
    gateway.WithReference(generator);
    gateway.WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
