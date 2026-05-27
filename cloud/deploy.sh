#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
BUILD_DIR="$SCRIPT_DIR/build"
ENV_FILE="${ENV_FILE:-$SCRIPT_DIR/.env}"

log() { echo -e "\033[1;34m[INFO]\033[0m  $*"; }
ok() { echo -e "\033[1;32m[OK]\033[0m    $*"; }
err() { echo -e "\033[1;31m[ERROR]\033[0m $*" >&2; exit 1; }

if [[ ! -f "$ENV_FILE" ]]; then
    err "Файл $ENV_FILE не найден. Скопируйте cloud/.env.example в cloud/.env и заполните параметры."
fi

set -a
source <(sed 's/\r$//' "$ENV_FILE")
set +a

require_command() {
    command -v "$1" >/dev/null 2>&1 || err "Не найдена команда '$1'"
}

require_env() {
    local name="$1"
    [[ -n "${!name:-}" ]] || err "В $ENV_FILE не задан параметр $name"
}

require_command yc
require_command dotnet
require_command zip
require_command python3

python3 - <<'PY' >/dev/null 2>&1 || err "Python-пакет boto3 не найден. Установите: python3 -m pip install boto3"
import boto3
PY

for var in \
    YC_CLOUD_ID \
    YC_FOLDER_ID \
    YC_REGION \
    YC_SERVICE_ACCOUNT_NAME \
    YC_CLIENT_BUCKET \
    YC_FILES_BUCKET \
    YC_QUEUE_NAME \
    YC_API_FUNCTION_NAME \
    YC_FILE_FUNCTION_NAME \
    YC_FILE_HTTP_FUNCTION_NAME \
    YC_API_GATEWAY_NAME
do
    require_env "$var"
done

mkdir -p "$BUILD_DIR"

log "Используется каталог Yandex Cloud: $YC_FOLDER_ID"

get_service_account_id() {
    yc iam service-account list --folder-id "$YC_FOLDER_ID" --format json \
        | python3 -c "import json,sys; name='$YC_SERVICE_ACCOUNT_NAME'; data=json.load(sys.stdin); print(next((x['id'] for x in data if x['name']==name), ''))"
}

SA_ID="$(get_service_account_id)"
if [[ -z "$SA_ID" ]]; then
    log "Создание сервисного аккаунта $YC_SERVICE_ACCOUNT_NAME"
    SA_ID="$(yc iam service-account create --name "$YC_SERVICE_ACCOUNT_NAME" --folder-id "$YC_FOLDER_ID" --format json \
        | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")"
    ok "Сервисный аккаунт создан: $SA_ID"
else
    ok "Сервисный аккаунт найден: $SA_ID"
fi

for role in storage.admin ymq.admin serverless.functions.invoker; do
    yc resource-manager folder add-access-binding "$YC_FOLDER_ID" \
        --role "$role" \
        --subject "serviceAccount:$SA_ID" \
        --quiet >/dev/null 2>&1 || true
done
ok "Роли сервисного аккаунта проверены"

if [[ -z "${YC_STATIC_KEY_ID:-}" || -z "${YC_STATIC_KEY_SECRET:-}" ]]; then
    cat >&2 <<EOF
В $ENV_FILE не заданы YC_STATIC_KEY_ID и/или YC_STATIC_KEY_SECRET.
Создайте статический ключ для сервисного аккаунта и добавьте его в cloud/.env:

yc iam access-key create --service-account-id "$SA_ID" --folder-id "$YC_FOLDER_ID"

EOF
    exit 1
fi

export SA_ID

log "Создание бакетов Object Storage"
python3 - <<'PY'
import os
import boto3
from botocore.exceptions import ClientError

s3 = boto3.client(
    "s3",
    endpoint_url="https://storage.yandexcloud.net",
    region_name=os.environ["YC_REGION"],
    aws_access_key_id=os.environ["YC_STATIC_KEY_ID"],
    aws_secret_access_key=os.environ["YC_STATIC_KEY_SECRET"],
)

def ensure_bucket(name, public=False, website=False):
    try:
        s3.create_bucket(Bucket=name)
    except ClientError as exc:
        code = exc.response.get("Error", {}).get("Code", "")
        if code not in ("BucketAlreadyExists", "BucketAlreadyOwnedByYou"):
            raise

    if public:
        s3.put_bucket_acl(Bucket=name, ACL="public-read")

    if website:
        s3.put_bucket_website(
            Bucket=name,
            WebsiteConfiguration={
                "IndexDocument": {"Suffix": "index.html"},
                "ErrorDocument": {"Key": "index.html"},
            },
        )

ensure_bucket(os.environ["YC_FILES_BUCKET"], public=False, website=False)
ensure_bucket(os.environ["YC_CLIENT_BUCKET"], public=True, website=True)
PY
ok "Бакеты готовы"

log "Создание очереди Yandex Message Queue: $YC_QUEUE_NAME"
QUEUE_URL="$(python3 - <<'PY'
import os
import sys
import boto3
from botocore.exceptions import ClientError

sqs = boto3.client(
    "sqs",
    endpoint_url="https://message-queue.api.cloud.yandex.net",
    region_name=os.environ["YC_REGION"],
    aws_access_key_id=os.environ["YC_STATIC_KEY_ID"],
    aws_secret_access_key=os.environ["YC_STATIC_KEY_SECRET"],
)

try:
    response = sqs.create_queue(QueueName=os.environ["YC_QUEUE_NAME"])
except ClientError as exc:
    code = exc.response.get("Error", {}).get("Code", "")
    if code not in ("QueueAlreadyExists", "QueueAlreadyOwnedByYou"):
        print(exc, file=sys.stderr)
        raise
    response = sqs.get_queue_url(QueueName=os.environ["YC_QUEUE_NAME"])

print(response["QueueUrl"])
PY
)"
export QUEUE_URL
QUEUE_ARN="$(python3 - <<'PY'
import os
import boto3

sqs = boto3.client(
    "sqs",
    endpoint_url="https://message-queue.api.cloud.yandex.net",
    region_name=os.environ["YC_REGION"],
    aws_access_key_id=os.environ["YC_STATIC_KEY_ID"],
    aws_secret_access_key=os.environ["YC_STATIC_KEY_SECRET"],
)

response = sqs.get_queue_attributes(
    QueueUrl=os.environ["QUEUE_URL"],
    AttributeNames=["QueueArn"],
)
print(response["Attributes"]["QueueArn"])
PY
)"
ok "Очередь готова: $QUEUE_URL"
ok "ARN очереди: $QUEUE_ARN"

zip_publish() {
    local project_path="$1"
    local publish_dir="$2"
    local zip_path="$3"

    rm -rf "$publish_dir"
    rm -f "$zip_path"

    dotnet publish "$project_path" -c Release -o "$publish_dir" --nologo
    (cd "$publish_dir" && zip -r "$zip_path" . -q)
}

ensure_function() {
    local function_name="$1"
    local function_id

    function_id="$(yc serverless function list --folder-id "$YC_FOLDER_ID" --format json \
        | python3 -c "import json,sys; name='$function_name'; data=json.load(sys.stdin); print(next((x['id'] for x in data if x['name']==name), ''))")"

    if [[ -z "$function_id" ]]; then
        function_id="$(yc serverless function create --name "$function_name" --folder-id "$YC_FOLDER_ID" --format json \
            | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")"
        ok "Cloud Function создана: $function_name ($function_id)" >&2
    else
        ok "Cloud Function найдена: $function_name ($function_id)" >&2
    fi

    echo "$function_id"
}

log "Сборка Cloud Function генератора"
API_ZIP="$BUILD_DIR/api-function.zip"
zip_publish "$ROOT_DIR/ContractGenerator.CloudApi.Function/ContractGenerator.CloudApi.Function.csproj" "$BUILD_DIR/api-publish" "$API_ZIP"

API_FUNCTION_ID="$(ensure_function "$YC_API_FUNCTION_NAME")"
yc serverless function version create \
    --function-name "$YC_API_FUNCTION_NAME" \
    --folder-id "$YC_FOLDER_ID" \
    --runtime dotnet8 \
    --entrypoint "ContractGenerator.CloudApi.Function.Handler" \
    --memory 256m \
    --execution-timeout 30s \
    --source-path "$API_ZIP" \
    --environment "YC_REGION=$YC_REGION" \
    --environment "YC_STATIC_KEY_ID=$YC_STATIC_KEY_ID" \
    --environment "YC_STATIC_KEY_SECRET=$YC_STATIC_KEY_SECRET" \
    --environment "YMQ_ENDPOINT=https://message-queue.api.cloud.yandex.net" \
    --environment "YMQ_QUEUE_URL=$QUEUE_URL" \
    --service-account-id "$SA_ID" >/dev/null
yc serverless function allow-unauthenticated-invoke "$YC_API_FUNCTION_NAME" --folder-id "$YC_FOLDER_ID" >/dev/null 2>&1 || true
ok "Генератор опубликован: $API_FUNCTION_ID"

log "Сборка Cloud Functions файлового сервиса"
FILE_ZIP="$BUILD_DIR/file-function.zip"
zip_publish "$ROOT_DIR/ContractGenerator.CloudFileService.Function/ContractGenerator.CloudFileService.Function.csproj" "$BUILD_DIR/file-publish" "$FILE_ZIP"

FILE_FUNCTION_ID="$(ensure_function "$YC_FILE_FUNCTION_NAME")"
yc serverless function version create \
    --function-name "$YC_FILE_FUNCTION_NAME" \
    --folder-id "$YC_FOLDER_ID" \
    --runtime dotnet8 \
    --entrypoint "ContractGenerator.CloudFileService.Function.QueueHandler" \
    --memory 256m \
    --execution-timeout 60s \
    --source-path "$FILE_ZIP" \
    --environment "YC_REGION=$YC_REGION" \
    --environment "YC_STATIC_KEY_ID=$YC_STATIC_KEY_ID" \
    --environment "YC_STATIC_KEY_SECRET=$YC_STATIC_KEY_SECRET" \
    --environment "YC_S3_ENDPOINT=https://storage.yandexcloud.net" \
    --environment "YC_FILES_BUCKET=$YC_FILES_BUCKET" \
    --service-account-id "$SA_ID" >/dev/null
ok "YMQ worker опубликован: $FILE_FUNCTION_ID"

FILE_HTTP_FUNCTION_ID="$(ensure_function "$YC_FILE_HTTP_FUNCTION_NAME")"
yc serverless function version create \
    --function-name "$YC_FILE_HTTP_FUNCTION_NAME" \
    --folder-id "$YC_FOLDER_ID" \
    --runtime dotnet8 \
    --entrypoint "ContractGenerator.CloudFileService.Function.FilesHandler" \
    --memory 256m \
    --execution-timeout 30s \
    --source-path "$FILE_ZIP" \
    --environment "YC_REGION=$YC_REGION" \
    --environment "YC_STATIC_KEY_ID=$YC_STATIC_KEY_ID" \
    --environment "YC_STATIC_KEY_SECRET=$YC_STATIC_KEY_SECRET" \
    --environment "YC_S3_ENDPOINT=https://storage.yandexcloud.net" \
    --environment "YC_FILES_BUCKET=$YC_FILES_BUCKET" \
    --service-account-id "$SA_ID" >/dev/null
yc serverless function allow-unauthenticated-invoke "$YC_FILE_HTTP_FUNCTION_NAME" --folder-id "$YC_FOLDER_ID" >/dev/null 2>&1 || true
ok "HTTP файловая функция опубликована: $FILE_HTTP_FUNCTION_ID"

TRIGGER_NAME="${YC_QUEUE_TRIGGER_NAME:-$YC_FILE_FUNCTION_NAME-trigger}"
TRIGGER_EXISTS="$(yc serverless trigger list --folder-id "$YC_FOLDER_ID" --format json \
    | python3 -c "import json,sys; name='$TRIGGER_NAME'; data=json.load(sys.stdin); print('yes' if any(x.get('name')==name for x in data) else '')")"

if [[ -z "$TRIGGER_EXISTS" ]]; then
    log "Создание YMQ-trigger: $TRIGGER_NAME"
    yc serverless trigger create message-queue \
        --name "$TRIGGER_NAME" \
        --folder-id "$YC_FOLDER_ID" \
        --queue "$QUEUE_ARN" \
        --queue-service-account-id "$SA_ID" \
        --invoke-function-id "$FILE_FUNCTION_ID" \
        --invoke-function-service-account-id "$SA_ID" \
        --batch-size 10 \
        --batch-cutoff 10s >/dev/null
    ok "YMQ-trigger создан"
else
    ok "YMQ-trigger уже существует: $TRIGGER_NAME"
fi

log "Развёртывание Yandex API Gateway"
GATEWAY_SPEC="$BUILD_DIR/api-gateway.yaml"
sed -e "s|\${API_FUNCTION_ID}|$API_FUNCTION_ID|g" \
    -e "s|\${FILE_HTTP_FUNCTION_ID}|$FILE_HTTP_FUNCTION_ID|g" \
    -e "s|\${SERVICE_ACCOUNT_ID}|$SA_ID|g" \
    "$SCRIPT_DIR/api-gateway.yaml" > "$GATEWAY_SPEC"

GATEWAY_ID="$(yc serverless api-gateway list --folder-id "$YC_FOLDER_ID" --format json \
    | python3 -c "import json,sys; name='$YC_API_GATEWAY_NAME'; data=json.load(sys.stdin); print(next((x['id'] for x in data if x['name']==name), ''))")"

if [[ -z "$GATEWAY_ID" ]]; then
    GATEWAY_ID="$(yc serverless api-gateway create \
        --name "$YC_API_GATEWAY_NAME" \
        --folder-id "$YC_FOLDER_ID" \
        --spec "$GATEWAY_SPEC" \
        --format json | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")"
    ok "API Gateway создан: $GATEWAY_ID"
else
    yc serverless api-gateway update "$YC_API_GATEWAY_NAME" \
        --folder-id "$YC_FOLDER_ID" \
        --spec "$GATEWAY_SPEC" >/dev/null
    ok "API Gateway обновлён: $GATEWAY_ID"
fi

GATEWAY_DOMAIN="$(yc serverless api-gateway get "$YC_API_GATEWAY_NAME" --folder-id "$YC_FOLDER_ID" --format json \
    | python3 -c "import json,sys; print(json.load(sys.stdin).get('domain', ''))")"
API_GATEWAY_URL="https://$GATEWAY_DOMAIN"
ok "API Gateway URL: $API_GATEWAY_URL"

log "Сборка Blazor WASM клиента"
CLIENT_PUBLISH_DIR="$BUILD_DIR/client-publish"
export CLIENT_PUBLISH_DIR
rm -rf "$CLIENT_PUBLISH_DIR"
dotnet publish "$ROOT_DIR/Client.Wasm/Client.Wasm.csproj" -c Release -o "$CLIENT_PUBLISH_DIR" --nologo
cat > "$CLIENT_PUBLISH_DIR/wwwroot/appsettings.json" <<EOF
{
  "BaseAddress": "$API_GATEWAY_URL/employee"
}
EOF
ok "Клиент собран"

log "Загрузка клиента в Object Storage: $YC_CLIENT_BUCKET"
python3 - <<'PY'
import mimetypes
import os
import boto3

s3 = boto3.client(
    "s3",
    endpoint_url="https://storage.yandexcloud.net",
    region_name=os.environ["YC_REGION"],
    aws_access_key_id=os.environ["YC_STATIC_KEY_ID"],
    aws_secret_access_key=os.environ["YC_STATIC_KEY_SECRET"],
)

wwwroot = os.path.join(os.environ["CLIENT_PUBLISH_DIR"], "wwwroot")
count = 0

for root, _, files in os.walk(wwwroot):
    for file_name in files:
        file_path = os.path.join(root, file_name)
        key = os.path.relpath(file_path, wwwroot).replace(os.sep, "/")
        extra = {"ACL": "public-read"}

        content_name = file_name
        if file_name.endswith(".br"):
            content_name = file_name[:-3]
            extra["ContentEncoding"] = "br"
        elif file_name.endswith(".gz"):
            content_name = file_name[:-3]
            extra["ContentEncoding"] = "gzip"

        content_type, _ = mimetypes.guess_type(content_name)
        extra["ContentType"] = content_type or ("application/wasm" if content_name.endswith(".wasm") else "application/octet-stream")

        with open(file_path, "rb") as body:
            s3.put_object(Bucket=os.environ["YC_CLIENT_BUCKET"], Key=key, Body=body.read(), **extra)
        count += 1

print(f"Uploaded files: {count}")
PY

CLIENT_URL="http://$YC_CLIENT_BUCKET.website.yandexcloud.net"
ok "Клиент опубликован: $CLIENT_URL"

cat > "$BUILD_DIR/deployment-info.json" <<EOF
{
  "apiGatewayUrl": "$API_GATEWAY_URL",
  "clientUrl": "$CLIENT_URL",
  "queueUrl": "$QUEUE_URL",
  "filesBucket": "$YC_FILES_BUCKET",
  "clientBucket": "$YC_CLIENT_BUCKET",
  "apiFunctionId": "$API_FUNCTION_ID",
  "fileFunctionId": "$FILE_FUNCTION_ID",
  "fileHttpFunctionId": "$FILE_HTTP_FUNCTION_ID",
  "serviceAccountId": "$SA_ID"
}
EOF

echo
ok "Развёртывание завершено"
echo "  API Gateway:  $API_GATEWAY_URL"
echo "  Клиент:       $CLIENT_URL"
echo "  Очередь:      $QUEUE_URL"
echo "  Бакет файлов: $YC_FILES_BUCKET"
