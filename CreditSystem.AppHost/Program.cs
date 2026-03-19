var builder = DistributedApplication.CreateBuilder(args);

// Используем Redis для кэширования (согласно общему заданию лабы №1)
var redis = builder.AddRedis("cache");

// Добавляем LocalStack для SQS (согласно варианту №30)
var localstack = builder.AddLocalStack("localstack")
    .WithServices("sqs");

// Наш основной API сервис (Query Based)
builder.AddProject<Projects.CreditSystem_Api>("creditsystem-api")
    .WithReference(redis)
    .WithReference(localstack) // Передаем ссылку на LocalStack/SQS
    .WaitFor(redis)
    .WaitFor(localstack);

builder.Build().Run();
