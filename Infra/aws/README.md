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
    "Region": "ap-southeast-1",
    "S3": {
      "BucketName": "e-verland-media",
      "BaseUrl": "https://cdn.e-verland.com"
    },
    "SQS": {
      "OrderEventsQueueUrl": "https://sqs.ap-southeast-1.amazonaws.com/.../orders"
    },
    "SNS": {
      "OrderNotificationsTopicArn": "arn:aws:sns:ap-southeast-1:...:orders"
    },
    "EventBridge": {
      "EventBusName": "e-verland-events"
    },
    "OpenSearch": {
      "Endpoint": "https://search-e-verland-....ap-southeast-1.es.amazonaws.com"
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
- `STORAGE_PROVIDER` (`S3` for R2-compatible deployments)
- `AWS_S3_BUCKET_NAME`
- `AWS_S3_BASE_URL`
- `AWS_S3_SERVICE_URL`
- `AWS_S3_ACCESS_KEY_ID`
- `AWS_S3_SECRET_ACCESS_KEY`
- `AWS_S3_FORCE_PATH_STYLE`
- `AWS_REGION`
