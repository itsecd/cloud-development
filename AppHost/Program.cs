var builder = DistributedApplication.CreateBuilder(args);

// Redis — кэш для сервиса генерации
var cache = builder.AddRedis("cache");

// Сервис генерации программных проектов (Bogus + Redis Cache + Serilog)
builder.AddProject<Projects.GenerationService>("generation-service")
    .WithReference(cache);

builder.Build().Run();
