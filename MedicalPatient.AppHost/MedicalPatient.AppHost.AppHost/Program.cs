var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var generator = builder.AddProject<Projects.MedicalPatient_Generator>("medicalpatient-generator")
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(generator)
    .WaitFor(generator);

builder.AddProject<Projects.MedicalPatient_ApiGateway>("medicalpatient-apigateway");

builder.Build().Run();

