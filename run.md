### инструкция к запуску

```bash
dotnet build
docker compose ps
docker compose up -d
sleep 15
docker compose exec localstack awslocal sns list-topics
docker compose exec localstack awslocal sqs list-queues
dotnet run --project AppHost
```
