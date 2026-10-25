#!/bin/bash
set -e

echo "Inicializando las colas SQS..."

# Crear la cola de mensajes fallidos
awslocal sqs create-queue \
  --queue-name cola-correos-dlq

# Obtener el ARN de la DLQ
DLQ_ARN=$(awslocal sqs get-queue-attributes \
  --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/cola-correos-dlq \
  --attribute-names QueueArn \
  --query 'Attributes.QueueArn' \
  --output text)

# Crear la cola principal
awslocal sqs create-queue \
  --queue-name cola-correos \
  --attributes VisibilityTimeout=60

# Configurar la política de reintentos
REDRIVE_POLICY=$(printf \
  '{"deadLetterTargetArn":"%s","maxReceiveCount":"3"}' \
  "$DLQ_ARN")

# Generar los atributos en un archivo JSON temporal
printf '{"RedrivePolicy":%s}\n' \
  "$(printf '%s' "$REDRIVE_POLICY" | python3 -c 'import json,sys; print(json.dumps(sys.stdin.read()))')" \
  > /tmp/atributos-cola.json

# Aplicar los atributos
awslocal sqs set-queue-attributes \
  --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/cola-correos \
  --attributes file:///tmp/atributos-cola.json

echo "Colas SQS inicializadas correctamente."