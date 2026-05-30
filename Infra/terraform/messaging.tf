# ─── Dead Letter Queues ───────────────────────────────────────────────────────

resource "aws_sqs_queue" "order_events_dlq" {
  name                      = "e-verland-order-events-dlq"
  message_retention_seconds = 1209600 # 14 days
}

resource "aws_sqs_queue" "payment_events_dlq" {
  name                      = "e-verland-payment-events-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "product_sync_dlq" {
  name                      = "e-verland-product-sync-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "notification_events_dlq" {
  name                      = "e-verland-notification-events-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "stock_reserve_dlq" {
  name                      = "e-verland-stock-reserve-dlq"
  message_retention_seconds = 1209600
}

# ─── Main SQS Queues ──────────────────────────────────────────────────────────

resource "aws_sqs_queue" "order_events" {
  name                       = "e-verland-order-events"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.order_events_dlq.arn
    maxReceiveCount     = 3
  })
}

resource "aws_sqs_queue" "payment_events" {
  name                       = "e-verland-payment-events"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.payment_events_dlq.arn
    maxReceiveCount     = 3
  })
}

resource "aws_sqs_queue" "product_sync" {
  name                       = "e-verland-product-sync"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.product_sync_dlq.arn
    maxReceiveCount     = 3
  })
}

resource "aws_sqs_queue" "notification_events" {
  name                       = "e-verland-notification-events"
  visibility_timeout_seconds = 30
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.notification_events_dlq.arn
    maxReceiveCount     = 3
  })
}

resource "aws_sqs_queue" "stock_reserve" {
  name                       = "e-verland-stock-reserve"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.stock_reserve_dlq.arn
    maxReceiveCount     = 3
  })
}

# ─── Shipping SQS Queues & DLQs ────────────────────────────────────────────────

resource "aws_sqs_queue" "shipping_draft_dlq" {
  name                      = "e-verland-shipping-draft-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "shipping_draft" {
  name                      = "e-verland-shipping-draft"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.shipping_draft_dlq.arn
    maxReceiveCount     = 3
  })
}

resource "aws_sqs_queue" "shipping_status_dlq" {
  name                      = "e-verland-shipping-status-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "shipping_status" {
  name                      = "e-verland-shipping-status"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.shipping_status_dlq.arn
    maxReceiveCount     = 3
  })
}

# ─── SQS Queue Policies (allow SNS to publish) ────────────────────────────────

resource "aws_sqs_queue_policy" "payment_events_policy" {
  queue_url = aws_sqs_queue.payment_events.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "sns.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.payment_events.arn
      Condition = {
        ArnEquals = { "aws:SourceArn" = aws_sns_topic.payment_events.arn }
      }
    }]
  })
}

resource "aws_sqs_queue_policy" "order_events_policy" {
  queue_url = aws_sqs_queue.order_events.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "sns.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.order_events.arn
      Condition = {
        ArnEquals = { "aws:SourceArn" = aws_sns_topic.order_events.arn }
      }
    }]
  })
}

resource "aws_sqs_queue_policy" "notification_events_policy" {
  queue_url = aws_sqs_queue.notification_events.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "sns.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.notification_events.arn
      Condition = {
        ArnEquals = { "aws:SourceArn" = aws_sns_topic.notification_events.arn }
      }
    }]
  })
}

# ─── SNS Topics ───────────────────────────────────────────────────────────────

resource "aws_sns_topic" "payment_events" {
  name = "e-verland-payment-events"
}

resource "aws_sns_topic" "order_events" {
  name = "e-verland-order-events"
}

resource "aws_sns_topic" "notification_events" {
  name = "e-verland-notification-events"
}

# ─── SNS → SQS Subscriptions ──────────────────────────────────────────────────

resource "aws_sns_topic_subscription" "payment_events_to_sqs" {
  topic_arn = aws_sns_topic.payment_events.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.payment_events.arn
}

resource "aws_sns_topic_subscription" "order_events_to_sqs" {
  topic_arn = aws_sns_topic.order_events.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.order_events.arn
}

resource "aws_sns_topic_subscription" "notification_events_to_sqs" {
  topic_arn = aws_sns_topic.notification_events.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.notification_events.arn
}

# ─── EventBridge Custom Bus ───────────────────────────────────────────────────

resource "aws_cloudwatch_event_bus" "main" {
  name = "e-verland-events"
}

# Preserve existing daily cleanup rule (on default bus)
resource "aws_cloudwatch_event_rule" "daily_task" {
  name                = "e-verland-daily-cleanup"
  schedule_expression = "cron(0 0 * * ? *)"
  event_bus_name      = "default"
}

# ─── Outputs ──────────────────────────────────────────────────────────────────

output "order_events_queue_url" {
  description = "SQS URL for order events"
  value       = aws_sqs_queue.order_events.url
}

output "payment_events_queue_url" {
  description = "SQS URL for payment events"
  value       = aws_sqs_queue.payment_events.url
}

output "product_sync_queue_url" {
  description = "SQS URL for product sync events"
  value       = aws_sqs_queue.product_sync.url
}

output "notification_events_queue_url" {
  description = "SQS URL for notification events"
  value       = aws_sqs_queue.notification_events.url
}

output "stock_reserve_queue_url" {
  description = "SQS URL for stock reservation requests"
  value       = aws_sqs_queue.stock_reserve.url
}

output "payment_events_topic_arn" {
  description = "SNS ARN for payment events topic"
  value       = aws_sns_topic.payment_events.arn
}

output "order_events_topic_arn" {
  description = "SNS ARN for order events topic"
  value       = aws_sns_topic.order_events.arn
}

output "notification_events_topic_arn" {
  description = "SNS ARN for notification events topic"
  value       = aws_sns_topic.notification_events.arn
}

output "product_events_topic_arn" {
  description = "SNS ARN for product events topic"
  value       = aws_sns_topic.product_events.arn
}

output "event_bus_name" {
  description = "EventBridge custom bus name"
  value       = aws_cloudwatch_event_bus.main.name
}

output "event_bus_arn" {
  description = "EventBridge custom bus ARN"
  value       = aws_cloudwatch_event_bus.main.arn
}
