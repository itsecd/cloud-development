# Идентификатор облака: yc config get cloud-id
export YC_CLOUD_ID="b1ghabr6tgumqv3n5tml"

# Идентификатор каталога: yc config get folder-id
export YC_FOLDER_ID="b1g067jv2vmp66m8c477"

# Регион YC
export YC_REGION="ru-central1"

# Имя сервисного аккаунта для функций, API Gateway, Object Storage и Message Queue
export SA_NAME="cloud-employee-sa"

# Бакет Object Storage для JSON-файлов кредитных заявок
export STORAGE_BUCKET="cloud-employee-files"

# Бакет Object Storage для Blazor WASM клиента
export CLIENT_BUCKET="cloud-employee-client"

# Yandex Message Queue
export QUEUE_NAME="employee-generated"

# Cloud Function генерации заявок
export API_FUNCTION_NAME="cloud-employee-generator"

# Cloud Function файлового сервиса
export FILE_FUNCTION_NAME="cloud-employee-file-service"

# API Gateway
export API_GATEWAY_NAME="cloud-employee-api-gateway"