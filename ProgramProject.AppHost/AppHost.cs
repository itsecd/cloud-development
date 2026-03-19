var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithRedisCommander();

// Сервис генерации теперь делится на 5 реплик
var generators = new List<IResourceBuilder<ProjectResource>>();

// Создаём 5 генераторов в цикле
for (var i = 1; i <= 5; i++)
{
    var generator = builder.AddProject<Projects.ProgramProject_GenerationService>($"generator-{i}")
        .WithExternalHttpEndpoints()
        .WithReference(cache)
        .WaitFor(cache)
        .WithEndpoint("http", endpoint => endpoint.Port = 6200 + i)
        .WithEndpoint("https", endpoint => endpoint.Port = 7200 + i);

    generators.Add(generator);
}


// Шлюз
var gateway = builder.AddProject<Projects.ProgramProject_Gateway>("gateway")
    .WithExternalHttpEndpoints();

foreach (var generator in generators)
{
    gateway.WaitFor(generator);
}

// Клиент теперь связывается с генератором через шлюз
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WaitFor(gateway);

builder.Build().Run();