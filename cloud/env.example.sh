# Скопируйте этот файл в env.sh и заполните значениями из вашего Yandex Cloud аккаунта.

# Идентификатор облака: yc config get cloud-id
export YC_CLOUD_ID="b1g..."

# Идентификатор каталога: yc config get folder-id
export YC_FOLDER_ID="b1g..."

# Регион YC
export YC_REGION="ru-central1"

# Имя сервисного аккаунта для функций, API Gateway, Object Storage и Message Queue
export SA_NAME="credit-application-sa"

# Бакет Object Storage для JSON-файлов кредитных заявок
export STORAGE_BUCKET="credit-application-files"

# Бакет Object Storage для Blazor WASM клиента
export CLIENT_BUCKET="credit-application-client"

# Yandex Message Queue
export QUEUE_NAME="credit-application-generated"

# Cloud Function генерации заявок
export API_FUNCTION_NAME="credit-application-generator"

# Cloud Function файлового сервиса
export FILE_FUNCTION_NAME="credit-application-file-service"

# API Gateway
export API_GATEWAY_NAME="credit-application-api-gateway"
