# Khai báo data source để lấy IP Cloudflare và Zone ID
data "cloudflare_ip_ranges" "cloudflare" {}

data "cloudflare_zone" "main" {
  filter = {
    name = var.domain_name
  }
}

# Security Group cho ALB - Chỉ cho phép Cloudflare
resource "aws_security_group" "alb_sg" {
  name        = "${var.project_name}-alb-sg"
  description = "Only allow traffic from Cloudflare"
  vpc_id      = module.vpc.vpc_id

  # Cổng 443 (HTTPS) - Chỉ nhận từ dải IP Cloudflare
  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = data.cloudflare_ip_ranges.cloudflare.ipv4_cidrs
  }

  # Cổng 80 (HTTP) - Redirect về HTTPS
  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = data.cloudflare_ip_ranges.cloudflare.ipv4_cidrs
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# Tạo bản ghi DNS trên Cloudflare trỏ về ALB
resource "cloudflare_dns_record" "api_endpoint" {
  zone_id = data.cloudflare_zone.main.id
  name    = "api"                # Sẽ tạo api.yourdomain.com
  content = aws_lb.main.dns_name # Lấy từ resource aws_lb của bạn
  type    = "CNAME"
  proxied = true # Bật Proxy (Đám mây cam)
  ttl     = 1
}

resource "aws_security_group" "ecs_sg" {
  name        = "${var.project_name}-ecs-sg"
  description = "Allow the ALB to reach the Fargate task"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb_sg.id]
  }

  ingress {
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.observability_sg.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_lb" "main" {
  name               = "${var.project_name}-alb"
  load_balancer_type = "application"
  subnets            = module.vpc.public_subnets
  security_groups    = [aws_security_group.alb_sg.id]
}

resource "aws_lb_target_group" "app" {
  name        = "${var.project_name}-tg"
  port        = 8080
  protocol    = "HTTP"
  target_type = "ip"
  vpc_id      = module.vpc.vpc_id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    unhealthy_threshold = 3
    interval            = 30
    path                = "/swagger/v1/swagger.json"
    matcher             = "200-399"
    timeout             = 5
  }
}

# HTTP listener — redirects all traffic to HTTPS
resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.main.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type = "redirect"
    redirect {
      port        = "443"
      protocol    = "HTTPS"
      status_code = "HTTP_301"
    }
  }
}

// Khai báo Data Source để Terraform tự đi tìm Certificate đã tạo
data "aws_acm_certificate" "e_verland_cert" {
  domain   = "*.${var.domain_name}"
  statuses = ["ISSUED"]
  most_recent = true
}

# HTTPS listener — terminates TLS and forwards to target group
resource "aws_lb_listener" "https" {
  load_balancer_arn = aws_lb.main.arn
  port              = 443
  protocol          = "HTTPS"
  ssl_policy        = "ELBSecurityPolicy-TLS13-1-2-2021-06"
  certificate_arn   = var.acm_certificate_arn

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.app.arn
  }
}

resource "aws_cloudwatch_log_group" "app" {
  name              = "/ecs/${var.project_name}-app"
  retention_in_days = 14
}

resource "aws_ecs_cluster" "prod" {
  name = "${var.project_name}-production"
}

resource "aws_ecr_repository" "app" {
  name = "${var.project_name}-backend"
}

locals {
  app_container_name = "e-verland-app"
  app_container_definitions = [
    {
      name      = local.app_container_name
      image     = var.container_image
      essential = true
      portMappings = [
        {
          containerPort = 8080
          hostPort      = 8080
          protocol      = "tcp"
        }
      ]
      environment = [
        { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
        { name = "ASPNETCORE_URLS", value = "http://+:8080" },
        { name = "App__BackendUrl", value = var.backend_url },
        { name = "App__FrontendUrl", value = var.frontend_url },
        { name = "Domain", value = var.domain_name },
        { name = "AWS_REGION", value = var.aws_region },

        # ─── ĐỒNG BỘ VỚI S3OPTIONS TRONG CODE C# ───
        { name = "Storage__Provider", value = var.storage_provider },
        { name = "Storage__BaseUrl", value = var.storage_base_url },
        { name = "AWS__S3__BucketName", value = var.s3_bucket_name },
        { name = "AWS__S3__Region", value = var.s3_region },
        { name = "AWS__S3__BaseUrl", value = var.storage_base_url },
        { name = "AWS__S3__ServiceUrl", value = var.s3_service_url },
        { name = "AWS__S3__ForcePathStyle", value = tostring(var.s3_force_path_style) },

        # ─── ĐỒNG BỘ CONFIG SQS (Lấy link từ các resource trong messaging.tf) ───
        { name = "AWS__SQS__OrderEventsQueueUrl", value = aws_sqs_queue.order_events.url },
        { name = "AWS__SQS__PaymentEventsQueueUrl", value = aws_sqs_queue.payment_events.url },
        { name = "AWS__SQS__ProductSyncQueueUrl", value = aws_sqs_queue.product_sync.url },
        { name = "AWS__SQS__ProductSyncDeadLetterQueueUrl", value = aws_sqs_queue.product_sync_dlq.url },
        { name = "AWS__SQS__MaxReceiveCount", value = "3" },

        # ─── ĐỒNG BỘ CONFIG SNS (Lấy ARN từ các resource trong messaging.tf) ───
        { name = "AWS__SNS__NotificationTopicArn", value = aws_sns_topic.notification_events.arn },
        { name = "AWS__SNS__OrderEventsTopicArn", value = aws_sns_topic.order_events.arn },
        { name = "AWS__SNS__PaymentEventsTopicArn", value = aws_sns_topic.payment_events.arn },
        { name = "AWS__SNS__ProductEventsTopicArn", value = aws_sns_topic.product_events.arn },

        # ─── ĐỒNG BỘ CONFIG EVENTBRIDGE (Lấy tên từ messaging.tf) ───
        { name = "AWS__EventBridge__EventBusName", value = aws_cloudwatch_event_bus.main.name },
        { name = "AWS__EventBridge__OrderEventSource", value = "e-verland.orders" },
        { name = "AWS__EventBridge__PaymentEventSource", value = "e-verland.payments" },
        { name = "AWS__EventBridge__ProductEventSource", value = "e-verland.products" }
      ]
      secrets = [
        { name = "ConnectionStrings__DefaultConnection", valueFrom = "${var.app_secrets_arn}:db_connection::" },
        { name = "Jwt__Key", valueFrom = "${var.app_secrets_arn}:jwt_key::" },
        { name = "AWS__S3__AccessKey", valueFrom = "${var.app_secrets_arn}:r2_access_key::" },
        { name = "AWS__S3__SecretKey", valueFrom = "${var.app_secrets_arn}:r2_secret_key::" },
        { name = "ConnectionStrings__ChatModule", valueFrom = "${var.app_secrets_arn}:mongodb_connection::" },
        { name = "ConnectionStrings__Redis", valueFrom = "${var.app_secrets_arn}:redis_connection::" }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = aws_cloudwatch_log_group.app.name
          awslogs-region        = var.aws_region
          awslogs-stream-prefix = "ecs"
        }
      }
    }
  ]
}

resource "aws_ecs_task_definition" "app" {
  family                   = "${var.project_name}-task"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = tostring(var.ecs_cpu)
  memory                   = tostring(var.ecs_memory)
  execution_role_arn       = aws_iam_role.app_role.arn
  task_role_arn            = aws_iam_role.app_role.arn
  container_definitions    = jsonencode(local.app_container_definitions)
}

resource "aws_ecs_service" "app" {
  name            = "${var.project_name}-app"
  cluster         = aws_ecs_cluster.prod.id
  task_definition = aws_ecs_task_definition.app.arn
  desired_count   = var.desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = module.vpc.private_subnets
    security_groups  = [aws_security_group.ecs_sg.id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.app.arn
    container_name   = local.app_container_name
    container_port   = 8080
  }

  service_registries {
    registry_arn = aws_service_discovery_service.app.arn
  }

  depends_on = [aws_lb_listener.https]
}
