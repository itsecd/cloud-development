var builder = DistributedApplication.CreateBuilder(args);

var localstack = builder.AddContainer("localstack", "localstack/localstack")
    .WithEnvironment("SERVICES", "sqs,s3")
    .WithEnvironment("DEFAULT_REGION", "us-east-1")
    .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
    .WithHttpEndpoint(port: 4566, targetPort: 4566);

var apiReplica1 = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api-r1")
    .WaitFor(localstack)
    .WithHttpEndpoint(port: 7001, name: "http");

var apiReplica2 = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api-r2")
    .WaitFor(localstack)
    .WithHttpEndpoint(port: 7002, name: "http");

var apiReplica3 = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api-r3")
    .WaitFor(localstack)
    .WithHttpEndpoint(port: 7003, name: "http");

var gateway = builder.AddProject<Projects.ProjectApp_Gateway>("projectapp-gateway")
    .WithReference(apiReplica1)
    .WithReference(apiReplica2)
    .WithReference(apiReplica3)
    .WaitFor(apiReplica1)
    .WaitFor(apiReplica2)
    .WaitFor(apiReplica3)
    .WithHttpEndpoint(port: 7000, name: "http");

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
