#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
BUILD_DIR="$SCRIPT_DIR/build"
ENV_FILE="$SCRIPT_DIR/env.sh"

if [ ! -f "$ENV_FILE" ]; then
  echo "[ERROR] Создайте cloud/env.sh на основе cloud/env.example.sh" >&2
  exit 1
fi

source "$ENV_FILE"
mkdir -p "$BUILD_DIR"

log() { echo -e "\033[1;34m[INFO]\033[0m  $*"; }
ok() { echo -e "\033[1;32m[OK]\033[0m    $*"; }
err() { echo -e "\033[1;31m[ERROR]\033[0m $*" >&2; exit 1; }

command -v yc >/dev/null 2>&1 || err "yc CLI не установлен"
command -v dotnet >/dev/null 2>&1 || err "dotnet SDK не найден"
command -v zip >/dev/null 2>&1 || err "zip не найден"
python3 - <<'PYEOF' >/dev/null 2>&1 || err "Установите boto3: python3 -m pip install boto3"
import boto3
PYEOF

log "Используем Yandex Cloud folder: $YC_FOLDER_ID"

SA_ID=$(yc iam service-account list --folder-id "$YC_FOLDER_ID" --format json \
  | python3 -c "import json,sys; d=json.load(sys.stdin); x=[i for i in d if i.get('name')=='$SA_NAME']; print(x[0]['id'] if x else '')")

if [ -z "$SA_ID" ]; then
  SA_ID=$(yc iam service-account create --name "$SA_NAME" --folder-id "$YC_FOLDER_ID" --format json \
    | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")
  ok "Сервисный аккаунт создан: $SA_ID"
else
  ok "Сервисный аккаунт уже существует: $SA_ID"
fi

for ROLE in storage.admin ymq.admin functions.functionInvoker; do
  yc resource-manager folder add-access-binding "$YC_FOLDER_ID" \
    --role "$ROLE" \
    --subject "serviceAccount:$SA_ID" >/dev/null
done
ok "Роли сервисного аккаунта проверены"

KEY_FILE="$BUILD_DIR/sa-access-key.json"
if [ ! -f "$KEY_FILE" ]; then
  yc iam access-key create --service-account-id "$SA_ID" --folder-id "$YC_FOLDER_ID" --format json > "$KEY_FILE"
fi

ACCESS_KEY_ID=$(python3 -c "import json; print(json.load(open('$KEY_FILE'))['access_key']['key_id'])")
ACCESS_KEY_SECRET=$(python3 -c "import json; print(json.load(open('$KEY_FILE'))['secret'])")
ok "S3/SQS access key готов"

log "Создание бакета файлов: $STORAGE_BUCKET"
python3 - <<PYEOF
import boto3
from botocore.exceptions import ClientError
s3 = boto3.client(
    "s3",
    endpoint_url="https://storage.yandexcloud.net",
    region_name="$YC_REGION",
    aws_access_key_id="$ACCESS_KEY_ID",
    aws_secret_access_key="$ACCESS_KEY_SECRET",
)
try:
    s3.create_bucket(Bucket="$STORAGE_BUCKET")
except ClientError as e:
    code = e.response["Error"]["Code"]
    if code not in ("BucketAlreadyExists", "BucketAlreadyOwnedByYou"):
        raise
PYEOF
ok "Бакет файлов готов"

log "Создание бакета клиента: $CLIENT_BUCKET"
python3 - <<PYEOF
import boto3
from botocore.exceptions import ClientError
s3 = boto3.client(
    "s3",
    endpoint_url="https://storage.yandexcloud.net",
    region_name="$YC_REGION",
    aws_access_key_id="$ACCESS_KEY_ID",
    aws_secret_access_key="$ACCESS_KEY_SECRET",
)
try:
    s3.create_bucket(Bucket="$CLIENT_BUCKET")
except ClientError as e:
    code = e.response["Error"]["Code"]
    if code not in ("BucketAlreadyExists", "BucketAlreadyOwnedByYou"):
        raise
s3.put_bucket_website(
    Bucket="$CLIENT_BUCKET",
    WebsiteConfiguration={
        "IndexDocument": {"Suffix": "index.html"},
        "ErrorDocument": {"Key": "index.html"},
    },
)
PYEOF
ok "Бакет клиента готов"

log "Создание Message Queue: $QUEUE_NAME"
QUEUE_INFO=$(python3 - <<PYEOF
import boto3
sqs = boto3.client(
    "sqs",
    endpoint_url="https://message-queue.api.cloud.yandex.net",
    region_name="$YC_REGION",
    aws_access_key_id="$ACCESS_KEY_ID",
    aws_secret_access_key="$ACCESS_KEY_SECRET",
)
queue_url = sqs.create_queue(QueueName="$QUEUE_NAME")["QueueUrl"]
queue_arn = sqs.get_queue_attributes(
    QueueUrl=queue_url,
    AttributeNames=["QueueArn"],
)["Attributes"]["QueueArn"]
print(queue_url)
print(queue_arn)
PYEOF
)
QUEUE_URL=$(echo "$QUEUE_INFO" | sed -n '1p')
QUEUE_ID=$(echo "$QUEUE_INFO" | sed -n '2p')
ok "Очередь готова: $QUEUE_URL"

log "Архивация Cloud Function генерации"
API_ZIP="$BUILD_DIR/api-function.zip"
rm -f "$API_ZIP"
(cd "$ROOT_DIR/Cloud.GeneratorFunction" && zip -q "$API_ZIP" *.cs *.csproj)
ok "Архив создан: $API_ZIP"

log "Деплой Cloud Function генерации: $API_FUNCTION_NAME"
yc serverless function create --name "$API_FUNCTION_NAME" --folder-id "$YC_FOLDER_ID" >/dev/null 2>&1 || true
API_FUNCTION_ID=$(yc serverless function get "$API_FUNCTION_NAME" --folder-id "$YC_FOLDER_ID" --format json \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")
yc serverless function version create \
  --function-name "$API_FUNCTION_NAME" \
  --folder-id "$YC_FOLDER_ID" \
  --runtime dotnet8 \
  --entrypoint "Cloud.GeneratorFunction.Handler" \
  --memory 256m \
  --execution-timeout 30s \
  --source-path "$API_ZIP" \
  --environment "SQS_ENDPOINT=https://message-queue.api.cloud.yandex.net" \
  --environment "SQS_QUEUE_URL=$QUEUE_URL" \
  --environment "AWS_ACCESS_KEY_ID=$ACCESS_KEY_ID" \
  --environment "AWS_SECRET_ACCESS_KEY=$ACCESS_KEY_SECRET" \
  --environment "YC_REGION=$YC_REGION" \
  --service-account-id "$SA_ID" >/dev/null
ok "Функция генерации готова: $API_FUNCTION_ID"

log "Архивация Cloud Function файлового сервиса"
FILE_ZIP="$BUILD_DIR/file-function.zip"
rm -f "$FILE_ZIP"
(cd "$ROOT_DIR/Cloud.FileServiceFunction" && zip -q "$FILE_ZIP" *.cs *.csproj)
ok "Архив создан: $FILE_ZIP"

log "Деплой Cloud Function файлового сервиса: $FILE_FUNCTION_NAME"
yc serverless function create --name "$FILE_FUNCTION_NAME" --folder-id "$YC_FOLDER_ID" >/dev/null 2>&1 || true
FILE_FUNCTION_ID=$(yc serverless function get "$FILE_FUNCTION_NAME" --folder-id "$YC_FOLDER_ID" --format json \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")
yc serverless function version create \
  --function-name "$FILE_FUNCTION_NAME" \
  --folder-id "$YC_FOLDER_ID" \
  --runtime dotnet8 \
  --entrypoint "Cloud.FileServiceFunction.Handler" \
  --memory 256m \
  --execution-timeout 60s \
  --source-path "$FILE_ZIP" \
  --environment "S3_ENDPOINT=https://storage.yandexcloud.net" \
  --environment "S3_BUCKET=$STORAGE_BUCKET" \
  --environment "AWS_ACCESS_KEY_ID=$ACCESS_KEY_ID" \
  --environment "AWS_SECRET_ACCESS_KEY=$ACCESS_KEY_SECRET" \
  --environment "YC_REGION=$YC_REGION" \
  --service-account-id "$SA_ID" >/dev/null
ok "Файловая функция готова: $FILE_FUNCTION_ID"

log "Создание Message Queue trigger"
TRIGGER_NAME="$FILE_FUNCTION_NAME-trigger"
TRIGGER_EXISTS=$(yc serverless trigger list --folder-id "$YC_FOLDER_ID" --format json \
  | python3 -c "import json,sys; d=json.load(sys.stdin); print('yes' if any(x.get('name')=='$TRIGGER_NAME' for x in d) else '')")
if [ -z "$TRIGGER_EXISTS" ]; then
  yc serverless trigger create message-queue \
    --name "$TRIGGER_NAME" \
    --folder-id "$YC_FOLDER_ID" \
    --queue "$QUEUE_ID" \
    --queue-service-account-id "$SA_ID" \
    --invoke-function-id "$FILE_FUNCTION_ID" \
    --invoke-function-service-account-id "$SA_ID" \
    --batch-size 10 \
    --batch-cutoff 10s >/dev/null
fi
ok "Message Queue trigger готов"

log "Деплой API Gateway: $API_GATEWAY_NAME"
GATEWAY_SPEC="$BUILD_DIR/api-gateway-rendered.yaml"
sed -e "s|\${FUNCTION_ID}|$API_FUNCTION_ID|g" \
    -e "s|\${SERVICE_ACCOUNT_ID}|$SA_ID|g" \
    "$SCRIPT_DIR/api-gateway.yaml" > "$GATEWAY_SPEC"

GATEWAY_ID=$(yc serverless api-gateway list --folder-id "$YC_FOLDER_ID" --format json \
  | python3 -c "import json,sys; d=json.load(sys.stdin); x=[i for i in d if i.get('name')=='$API_GATEWAY_NAME']; print(x[0]['id'] if x else '')")
if [ -z "$GATEWAY_ID" ]; then
  GATEWAY_ID=$(yc serverless api-gateway create \
    --name "$API_GATEWAY_NAME" \
    --folder-id "$YC_FOLDER_ID" \
    --spec "$GATEWAY_SPEC" \
    --format json | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")
else
  yc serverless api-gateway update "$API_GATEWAY_NAME" \
    --folder-id "$YC_FOLDER_ID" \
    --spec "$GATEWAY_SPEC" >/dev/null
fi
GATEWAY_DOMAIN=$(yc serverless api-gateway get "$API_GATEWAY_NAME" --folder-id "$YC_FOLDER_ID" --format json \
  | python3 -c "import json,sys; print(json.load(sys.stdin).get('domain',''))")
API_GATEWAY_URL="https://$GATEWAY_DOMAIN"
ok "API Gateway готов: $API_GATEWAY_URL"

log "Сборка Blazor WASM клиента"
CLIENT_SETTINGS="$ROOT_DIR/Client.Wasm/wwwroot/appsettings.json"
cat > "$CLIENT_SETTINGS" <<EOF
{
  "BaseAddress": "$API_GATEWAY_URL/"
}
EOF
dotnet publish "$ROOT_DIR/Client.Wasm/Client.Wasm.csproj" -c Release -o "$BUILD_DIR/client-publish" --nologo
ok "Клиент собран"

log "Загрузка клиента в Object Storage"
python3 - <<PYEOF
import boto3
import mimetypes
import os
s3 = boto3.client(
    "s3",
    endpoint_url="https://storage.yandexcloud.net",
    region_name="$YC_REGION",
    aws_access_key_id="$ACCESS_KEY_ID",
    aws_secret_access_key="$ACCESS_KEY_SECRET",
)
wwwroot = "$BUILD_DIR/client-publish/wwwroot"
count = 0
for root, _, files in os.walk(wwwroot):
    for filename in files:
        path = os.path.join(root, filename)
        key = path[len(wwwroot) + 1:]
        content_type, _ = mimetypes.guess_type(filename)
        if content_type is None:
            content_type = "application/wasm" if filename.endswith(".wasm") else "application/octet-stream"
        extra = {"ContentType": content_type}
        if filename.endswith(".br"):
            extra["ContentEncoding"] = "br"
        elif filename.endswith(".gz"):
            extra["ContentEncoding"] = "gzip"
        with open(path, "rb") as stream:
            s3.put_object(Bucket="$CLIENT_BUCKET", Key=key, Body=stream.read(), ACL="public-read", **extra)
        count += 1
print(count)
PYEOF

CLIENT_URL="http://$CLIENT_BUCKET.website.yandexcloud.net"
ok "Клиент опубликован: $CLIENT_URL"

cat > "$BUILD_DIR/deployment-info.json" <<EOF
{
  "apiGatewayUrl": "$API_GATEWAY_URL",
  "clientUrl": "$CLIENT_URL",
  "queueUrl": "$QUEUE_URL",
  "storageBucket": "$STORAGE_BUCKET",
  "clientBucket": "$CLIENT_BUCKET",
  "apiFunctionId": "$API_FUNCTION_ID",
  "fileFunctionId": "$FILE_FUNCTION_ID",
  "serviceAccountId": "$SA_ID"
}
EOF

echo ""
echo "Развёртывание завершено"
echo "  API Gateway:  $API_GATEWAY_URL"
echo "  Клиент:       $CLIENT_URL"
echo "  Очередь:      $QUEUE_URL"
echo "  Бакет файлов: $STORAGE_BUCKET"
echo "  Бакет клиента: $CLIENT_BUCKET"