#!/bin/sh
set -e

REGION="${AWS_DEFAULT_REGION:-us-east-1}"
BUCKET_NAME="${LOCALSTACK_BUCKET_NAME:-totem-local-bucket}"
QUEUE_NAME="${LOCALSTACK_QUEUE_NAME:-totem-events-queue}"
TOPIC_NAME="${LOCALSTACK_TOPIC_NAME:-totem-events-topic}"
DB_INSTANCE_ID="${LOCALSTACK_RDS_INSTANCE_ID:-totem-db}"
DB_NAME="${LOCALSTACK_RDS_DB_NAME:-totem}"

echo "[localstack-init] creating S3 bucket: ${BUCKET_NAME}"
awslocal s3api create-bucket \
  --bucket "${BUCKET_NAME}" \
  --region "${REGION}" >/dev/null 2>&1 || true

echo "[localstack-init] creating SQS queue: ${QUEUE_NAME}"
awslocal sqs create-queue \
  --queue-name "${QUEUE_NAME}" \
  --region "${REGION}" >/dev/null 2>&1 || true

echo "[localstack-init] creating SNS topic: ${TOPIC_NAME}"
awslocal sns create-topic \
  --name "${TOPIC_NAME}" \
  --region "${REGION}" >/dev/null 2>&1 || true

echo "[localstack-init] creating RDS instance: ${DB_INSTANCE_ID}"
awslocal rds create-db-instance \
  --db-instance-identifier "${DB_INSTANCE_ID}" \
  --db-instance-class db.t3.micro \
  --engine postgres \
  --master-username localstack \
  --master-user-password localstack \
  --allocated-storage 20 \
  --db-name "${DB_NAME}" \
  --region "${REGION}" >/dev/null 2>&1 || true

echo "[localstack-init] bootstrap finished"
