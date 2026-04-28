resource "aws_sns_topic" "user_updates" { name = "everland-user-updates" }
resource "aws_sqs_queue" "order_queue" { name = "everland-order-queue" }

resource "aws_cloudwatch_event_rule" "daily_task" {
  name                = "everland-daily-cleanup"
  schedule_expression = "cron(0 0 * * ? *)"
}