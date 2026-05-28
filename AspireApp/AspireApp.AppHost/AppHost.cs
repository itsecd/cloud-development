using Aspire.Hosting.LocalStack;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("employee-cache")
    .WithRedisInsight(containerName: "employee-insight");

var localstack = builder.AddLocalStack("localstack", configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
})
    ?? throw new InvalidOperationException(
        "LocalStack отключён в конфигурации AppHost (LocalStack:UseLocalStack = false). " +
        "Включите его в appsettings.json, иначе file-service и service-api не получат своего edge-эндпойнта.");

var localstackEndpoint = localstack.Resource.ConnectionStringExpression;

var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway");

var replicaWeights = new[] { 1, 2, 3, 2, 1 };

const int fileServicePort = 16000;

// WaitFor(localstack) не используем: health-check интеграции бывает «unhealthy» дольше,
// чем нужно, и блокирует старт зависимых сервисов. SnsFileExportWorker и SnsEmployeeEventPublisher
// уже имеют retry-цикл и сами дождутся готовности LocalStack.
var fileService = builder.AddProject<Projects.File_Service>("file-service", launchProfileName: null)
    .WithHttpEndpoint(port: fileServicePort, name: "http")
    .WithEnvironment("Aws__ServiceUrl", localstackEndpoint);

// NotificationEndpoint собираем уже после описания endpoint, чтобы порт
// можно было резолвить динамически (важно для Aspire.Testing,
// где фиксированный 16000 может быть переопределён).
var fileServiceHttp = fileService.GetEndpoint("http");
fileService.WithEnvironment("Aws__NotificationEndpoint", ReferenceExpression.Create(
    $"http://host.docker.internal:{fileServiceHttp.Property(EndpointProperty.Port)}/sns/notifications"));

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.Service_Api>($"service-api-{i}", launchProfileName: null)
        .WithHttpEndpoint(port: 15000 + i)
        .WithReference(cache, "RedisCache")
        .WithEnvironment("ReplicaId", "R" + (i + 1))
        .WithEnvironment("ReplicaWeight", replicaWeights[i].ToString())
        .WithEnvironment("Aws__ServiceUrl", localstackEndpoint)
        .WaitFor(cache);

    // Пробрасываем реальный порт в gateway: в Aspire тест-среде порты могут
    // выделяться динамически, поэтому захардкоженные значения из ocelot.json
    // могут не совпадать с фактическими. Env-переменные перекроют их после
    // повторного AddEnvironmentVariables() в ApiGateway/Program.cs.
    var serviceHttp = service.GetEndpoint("http");
    gateway
        .WithEnvironment(
            $"Routes__0__DownstreamHostAndPorts__{i}__Port",
            ReferenceExpression.Create($"{serviceHttp.Property(EndpointProperty.Port)}"))
        .WithEnvironment(
            $"WeightedRoundRobin__Nodes__{i}__Port",
            ReferenceExpression.Create($"{serviceHttp.Property(EndpointProperty.Port)}"));

    gateway.WaitFor(service);
}

gateway.WaitFor(fileService);

builder.AddProject<Projects.Client_Wasm>("employee")
    .WaitFor(gateway);

builder.Build().Run();
