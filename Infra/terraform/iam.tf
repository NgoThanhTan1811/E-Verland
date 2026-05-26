resource "aws_iam_role" "app_role" {
  name = "e-verland-app-service-role"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = ["ec2.amazonaws.com", "ecs-tasks.amazonaws.com"]
      }
    }]
  })
}

# ECS Task Execution Role Policy (must-have)
resource "aws_iam_role_policy_attachment" "ecs_execution_role" {
  role       = aws_iam_role.app_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# CloudWatch & X-Ray (observability)
resource "aws_iam_role_policy_attachment" "xray_write_access" {
  role       = aws_iam_role.app_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSXRayDaemonWriteAccess"
}

resource "aws_iam_role_policy_attachment" "cloudwatch_agent" {
  role       = aws_iam_role.app_role.name
  policy_arn = "arn:aws:iam::aws:policy/CloudWatchAgentServerPolicy"
}

# Custom policy for R2/S3 access (least-privilege)
resource "aws_iam_role_policy" "app_s3_policy" {
  name = "e-verland-app-s3-policy"
  role = aws_iam_role.app_role.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject"
        ]
        Resource = [
          "arn:aws:s3:::${var.s3_bucket_name}/*"
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "s3:ListBucket"
        ]
        Resource = [
          "arn:aws:s3:::${var.s3_bucket_name}"
        ]
      }
    ]
  })
}

# Custom policy for SQS (only specific queues)
resource "aws_iam_role_policy" "app_sqs_policy" {
  name = "e-verland-app-sqs-policy"
  role = aws_iam_role.app_role.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "sqs:SendMessage",
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:GetQueueAttributes"
        ]
        Resource = [
          "arn:aws:sqs:${var.aws_region}:*:e-verland-*"
        ]
      }
    ]
  })
}

# Custom policy for SNS (only specific topics)
resource "aws_iam_role_policy" "app_sns_policy" {
  name = "e-verland-app-sns-policy"
  role = aws_iam_role.app_role.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "sns:Publish"
        ]
        Resource = [
          "arn:aws:sns:${var.aws_region}:*:e-verland-*"
        ]
      }
    ]
  })
}

# Custom policy for EventBridge
resource "aws_iam_role_policy" "app_eventbridge_policy" {
  name = "e-verland-app-eventbridge-policy"
  role = aws_iam_role.app_role.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "events:PutEvents"
        ]
        Resource = [
          "arn:aws:events:${var.aws_region}:*:event-bus/e-verland-events"
        ]
      }
    ]
  })
}

# Custom policy for Secrets Manager (read-only specific secrets)
resource "aws_iam_role_policy" "app_secrets_policy" {
  name = "e-verland-app-secrets-policy"
  role = aws_iam_role.app_role.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        Resource = [
          "arn:aws:secretsmanager:${var.aws_region}:*:secret:e-verland/*"
        ]
      }
    ]
  })
}

resource "aws_iam_instance_profile" "app_profile" {
  name = "e-verland-instance-profile"
  role = aws_iam_role.app_role.name
}

