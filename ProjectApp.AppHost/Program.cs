var builder = DistributedApplication.CreateBuilder(args);

var localstack = builder.AddContainer("localstack", "localstack/localstack")
    .WithEnvironment("SERVICES", "sqs,s3")
    .WithEnvironment("DEFAULT_REGION", "us-east-1")
    .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
    .WithHttpEndpoint(port: 4566, targetPort: 4566);

var api = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api")
    .WaitFor(localstack)
    .WithReplicas(3);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
