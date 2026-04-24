# AWS Infrastructure Services

This directory contains AWS service implementations following clean architecture principles.

## Services Implemented

### Storage

- **S3** - Object storage for media files (images, videos)
  - Upload/Delete operations
  - Pre-signed URL generation
  - File metadata retrieval

### Messaging

- **SQS** - Message queuing for async processing
  - Order events queue
  - Payment notifications queue
  - Email queue
  - Batch send/receive operations

- **SNS** - Pub/sub notifications
  - Topic management
  - SMS sending
  - Email notifications
  - Push notifications

- **EventBridge** - Event-driven architecture
  - Domain event routing
  - Cross-module communication
  - Event schema management

### Monitoring & Observability

- **CloudWatch** - Logging and metrics
  - Custom metrics via Serilog EMF logs
  - Log groups
  - Alarms

- **X-Ray** - Distributed tracing
  - Request tracing
  - Performance profiling
  - Service maps

### Search

- **OpenSearch** - Full-text search
  - Product search
  - Order search
  - User search
  - Aggregations

## Architecture

Each service follows clean architecture:

```
Infra/aws/
├── {Service}Options.cs       # Configuration
├── I{Service}Service.cs      # Interface (abstraction)
└── {Service}Service.cs       # Implementation
```

## Configuration

Add to `appsettings.json`:

```json
{
  "AWS": {
    "Region": "us-east-1",
    "S3": {
      "BucketName": "e-verland-media",
      "BaseUrl": "https://cdn.e-verland.com"
    },
    "SQS": {
      "OrderEventsQueueUrl": "https://sqs.us-east-1.amazonaws.com/.../orders"
    },
    "SNS": {
      "OrderNotificationsTopicArn": "arn:aws:sns:us-east-1:...:orders"
    },
    "EventBridge": {
      "EventBusName": "e-verland-events"
    },
    "OpenSearch": {
      "Endpoint": "https://search-e-verland-....us-east-1.es.amazonaws.com"
    }
  }
}
```

## Required Environment Variables

## EMF Metrics via Serilog

Metrics are emitted as EMF JSON logs using Serilog instead of calling CloudWatch `PutMetricData`.

Flow:

1. App writes EMF log line containing `_aws.CloudWatchMetrics`.
2. CloudWatch Logs ingests the JSON line.
3. CloudWatch extracts metric values automatically.
4. Dashboards and alarms read these generated metrics.

## Required Environment Variables

Use these environment variables when you do not store secrets in appsettings files:

- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_REGION`
- `AWS_SQS_ORDER_EVENTS_QUEUE_URL`
- `AWS_SQS_PAYMENT_EVENTS_QUEUE_URL`
- `AWS_SQS_PRODUCT_SYNC_QUEUE_URL`
- `AWS_SQS_PRODUCT_SYNC_DLQ_URL`
- `AWS_SNS_NOTIFICATION_TOPIC_ARN`
- `AWS_SNS_ORDER_EVENTS_TOPIC_ARN`
- `AWS_SNS_PAYMENT_EVENTS_TOPIC_ARN`
- `AWS_SNS_PRODUCT_EVENTS_TOPIC_ARN`
- `STORAGE_PROVIDER` (`MinIO` or `S3`)

When `STORAGE_PROVIDER=MinIO`:

- `MINIO_ENDPOINT`
- `MINIO_ACCESS_KEY`
- `MINIO_SECRET_KEY`
- `MINIO_BUCKET_NAME`

When `STORAGE_PROVIDER=S3`:

- `AWS:S3:BucketName` from appsettings or `AWS_S3_BUCKET_NAME` if you add custom mapping in your deployment environment.
