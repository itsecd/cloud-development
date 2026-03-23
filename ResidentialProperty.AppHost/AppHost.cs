var builder = DistributedApplication.CreateBuilder(args);

// Добавляем Redis
var redis = builder.AddRedis("redis")
    .WithRedisCommander();

// Добавляем API проект с ссылкой на Redis
var api = builder.AddProject<Projects.ResidentialProperty_Api>("residentialproperty-api")
    .WithReference(redis)
    .WaitFor(redis); // ждем запуска Redis

// Добавляем клиентский проект
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(api); // ждем запуска API

builder.Build().Run();