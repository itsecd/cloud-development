#!/bin/bash
echo "Initializing LocalStack..."

# Создаём SNS topic
awslocal sns create-topic --name employee-topic

# Создаём SQS queue
awslocal sqs create-queue --queue-name employee-queue

# Подписываем SQS на SNS
awslocal sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:000000000000:employee-topic \
  --protocol sqs \
  --notification-endpoint arn:aws:sqs:us-east-1:000000000000:employee-queue

echo "LocalStack initialization complete!"
echo "SNS Topic: arn:aws:sns:us-east-1:000000000000:employee-topic"
echo "SQS Queue: http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/employee-queue"
