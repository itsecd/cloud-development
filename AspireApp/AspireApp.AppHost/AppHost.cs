using Aspire.Hosting.LocalStack;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("employee-cache")
    .WithRedisInsight(containerName: "employee-insight");

// LocalStack поднимаем через интеграцию LocalStack.Aspire.Hosting — Aspire управляет
// жизненным циклом контейнера. Закрепляем версию 3.5: в 3.7.2+ есть известный баг
// с подтверждением HTTP SNS-подписок (https://github.com/localstack/localstack/issues/11652).
var localstack = builder.AddLocalStack("localstack", configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.ContainerImageTag = "3.5";
})
    ?? throw new InvalidOperationException(
        "LocalStack отключён в конфигурации AppHost (LocalStack:UseLocalStack = false). " +
        "Включите его в appsettings.json, иначе file-service и service-api не получат своего edge-эндпойнта.");

// LocalStack.Aspire.Hosting всегда проксирует edge-порт 4566 через случайный хостовый порт,
// поэтому ServiceURL приходится резолвить динамически — иначе AWS-клиенты на хосте промахнутся.
// ILocalStackResource не реализует IResourceWithEndpoints напрямую, но под капотом это
// ContainerResource — приводим к нему, чтобы можно было дотянуться до edge-эндпойнта.
var localstackContainer = (IResourceBuilder<ContainerResource>)(object)localstack;
var localstackHttp = localstackContainer.GetEndpoint("http");
var localstackEndpoint = ReferenceExpression.Create(
    $"http://localhost:{localstackHttp.Property(EndpointProperty.Port)}");

var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway");

var replicaWeights = new[] { 1, 2, 3, 2, 1 };

const int fileServicePort = 16000;

// WaitFor(localstack) не используем: health-check интеграции бывает «unhealthy» дольше,
// чем нужно, и блокирует старт зависимых сервисов. SnsFileExportWorker и SnsEmployeeEventPublisher
// уже имеют retry-цикл и сами дождутся готовности LocalStack.
var fileService = builder.AddProject<Projects.File_Service>("file-service", launchProfileName: null)
    .WithHttpEndpoint(port: fileServicePort, name: "http")
    .WithEnvironment("Aws__ServiceUrl", localstackEndpoint)
    .WithEnvironment("Aws__Region", "us-east-1")
    .WithEnvironment("Aws__AccessKey", "test")
    .WithEnvironment("Aws__SecretKey", "test")
    .WithEnvironment("Aws__TopicName", "employee-generated-topic")
    .WithEnvironment("Aws__BucketName", "employee-files");

// NotificationEndpoint собираем уже после описания endpoint, чтобы порт
// можно было резолвить динамически (важно для Aspire.Testing,
// где фиксированный 16000 может быть переопределён).
var fileServiceHttp = fileService.GetEndpoint("http");
fileService.WithEnvironment("Aws__NotificationEndpoint", ReferenceExpression.Create(
    $"http://host.docker.internal:{fileServiceHttp.Property(EndpointProperty.Port)}/sns/notifications"));

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.Service_Api>($"service-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 15000 + i)
        .WithReference(cache, "RedisCache")
        .WithEnvironment("ReplicaId", "R" + (i + 1))
        .WithEnvironment("ReplicaWeight", replicaWeights[i].ToString())
        .WithEnvironment("Aws__ServiceUrl", localstackEndpoint)
        .WithEnvironment("Aws__Region", "us-east-1")
        .WithEnvironment("Aws__AccessKey", "test")
        .WithEnvironment("Aws__SecretKey", "test")
        .WithEnvironment("Aws__TopicName", "employee-generated-topic")
        .WaitFor(cache);

    gateway.WaitFor(service);
}

gateway.WaitFor(fileService);

builder.AddProject<Projects.Client_Wasm>("employee")
    .WaitFor(gateway);

builder.Build().Run();
