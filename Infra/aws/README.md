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
  - Custom metrics
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
