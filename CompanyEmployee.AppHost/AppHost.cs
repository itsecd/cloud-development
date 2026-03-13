var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis").WithRedisInsight();

var api = builder.AddProject<Projects.CompanyEmployee_ApiService>("companyemployee-api")
    .WithReference(redis)
    .WaitFor(redis)
    .WithUrlForEndpoint("http", e => e.Url = "/swagger")
    .WithUrlForEndpoint("https", e => e.Url = "/swagger");

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();