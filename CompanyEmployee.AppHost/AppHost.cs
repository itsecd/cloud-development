var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis").WithRedisInsight();

var api = builder.AddProject<Projects.CompanyEmployee_ApiGateway>("companyemployee-apigateway")
    .WithHttpEndpoint(name: "gateway", port: 5212);

for (var i = 1; i <= 3; i++)
{
    var generator = builder
        .AddProject<Projects.CompanyEmployee_ApiService>($"generator-{i}")
        .WithReference(redis)
        .WaitFor(redis)
        .WithHttpEndpoint(name: $"http");

    api.WithReference(generator)
        .WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();