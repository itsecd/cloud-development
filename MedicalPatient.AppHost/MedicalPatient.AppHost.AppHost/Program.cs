var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var generator1 = builder.AddProject<Projects.MedicalPatient_Generator>("generator-1")
    .WithEndpoint("http", endpoint => endpoint.Port = 5101)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

var generator2 = builder.AddProject<Projects.MedicalPatient_Generator>("generator-2")
    .WithEndpoint("http", endpoint => endpoint.Port = 5102)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

var generator3 = builder.AddProject<Projects.MedicalPatient_Generator>("generator-3")
    .WithEndpoint("http", endpoint => endpoint.Port = 5103)
    .WithReference(redis)
    .WaitFor(redis)
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

builder.Build().Run();

