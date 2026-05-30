# Khai báo data source để lấy IP Cloudflare và Zone ID
data "cloudflare_ip_ranges" "cloudflare" {}

data "cloudflare_zone" "main" {
    zone_id = var.zone_id
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

locals {
  subdomains = ["api", "seller", "admin"]
}

resource "cloudflare_dns_record" "subdomains" {
  for_each = toset(local.subdomains)

  zone_id = data.cloudflare_zone.main.id
  name    = each.value
  content = aws_lb.main.dns_name
  type    = "CNAME"
  proxied = true
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
    path                = "/health"
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
  domain   = "*.e-verland.site"
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
  app_container_definitions = [
    {
      name  = "e-verland-app"
      image = var.container_image
      portMappings = [{ containerPort = 8080, hostPort = 8080, protocol = "tcp" }]
      environment = [
        { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
        { name = "MeiliSearch__Host", value = "http://localhost:7700" }
      ]
      essential = true
    },
    {
      name  = "meilisearch"
      image = "getmeili/meilisearch:v1.7"
      portMappings = [{ containerPort = 7700, hostPort = 7700, protocol = "tcp" }]
      essential = true
      mountPoints = [
        { sourceVolume = "meili_data", containerPath = "/meili_data" }
      ]
    }
  ]

  
      
      essential = true

      # Các biến môi trường công khai
      environment = [
        { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
        { name = "ASPNETCORE_URLS", value = "http://+:8080" },
        { name = "App__BackendUrl", value = var.backend_url },
        { name = "App__FrontendUrl", value = var.frontend_url },
        { name = "Domain", value = var.domain_name },
        { name = "AWS_REGION", value = var.aws_region },
        { name = "Storage__Provider", value = var.storage_provider },
        { name = "AWS__S3__BucketName", value = var.s3_bucket_name },
        { name = "AWS__S3__Region", value = var.s3_region },
        { name = "AWS__S3__ForcePathStyle", value = tostring(var.s3_force_path_style) },
        # Lưu ý: MaxReceiveCount không có valueFrom nên để ở environment
        { name = "AWS__SQS__MaxReceiveCount", value = "3" }
      ]

      # Các biến nhạy cảm lấy từ Secrets Manager
      secrets = [
        # SQS URLs
        { name = "AWS__SQS__OrderEventsQueueUrl", valueFrom = "${var.app_secrets_arn}:AWS__SQS__OrderEventsQueueUrl::" },
        { name = "AWS__SQS__PaymentEventsQueueUrl", valueFrom = "${var.app_secrets_arn}:AWS__SQS__PaymentEventsQueueUrl::" },
        { name = "AWS__SQS__ProductSyncQueueUrl", valueFrom = "${var.app_secrets_arn}:AWS__SQS__ProductSyncQueueUrl::" },
        { name = "AWS__SQS__ProductSyncDeadLetterQueueUrl", valueFrom = "${var.app_secrets_arn}:AWS__SQS__ProductSyncDeadLetterQueueUrl::" },
        { name = "AWS__SQS__NotificationEventsQueueUrl", valueFrom = "${var.app_secrets_arn}:AWS__SQS__NotificationEventsQueueUrl::" },
        { name = "AWS__SQS__StockReserveQueueUrl", valueFrom = "${var.app_secrets_arn}:AWS__SQS__StockReserveQueueUrl::" },
        # SNS ARNs
        { name = "AWS__SNS__NotificationTopicArn", valueFrom = "${var.app_secrets_arn}:AWS__SNS__NotificationTopicArn::" },
        { name = "AWS__SNS__OrderEventsTopicArn", valueFrom = "${var.app_secrets_arn}:AWS__SNS__OrderEventsTopicArn::" },
        { name = "AWS__SNS__PaymentEventsTopicArn", valueFrom = "${var.app_secrets_arn}:AWS__SNS__PaymentEventsTopicArn::" },
        { name = "AWS__SNS__ProductEventsTopicArn", valueFrom = "${var.app_secrets_arn}:AWS__SNS__ProductEventsTopicArn::" },
        # Credentials & DBs
        { name = "Jwt__Key", valueFrom = "${var.app_secrets_arn}:Jwt__Key::" },
        { name = "AWS__S3__ServiceUrl", valueFrom = "${var.app_secrets_arn}:AWS__S3__ServiceUrl::" },
        { name = "AWS__S3__AccessKey", valueFrom = "${var.app_secrets_arn}:AWS__S3__AccessKey::" },
        { name = "AWS__S3__SecretKey", valueFrom = "${var.app_secrets_arn}:AWS__S3__SecretKey::" },
        { name = "Jwt__Issuer", valueFrom = "${var.app_secrets_arn}:Jwt__Issuer::" },
        { name = "Jwt__Audience", valueFrom = "${var.app_secrets_arn}:Jwt__Audience::" },
        # Database connection strings
        { name = "ConnectionStrings__UserDb", valueFrom = "${var.app_secrets_arn}:ConnectionStrings__UserDb::" },
        { name = "ConnectionStrings__AuthDb", valueFrom = "${var.app_secrets_arn}:ConnectionStrings__AuthDb::" },
        { name = "ConnectionStrings__PaymentDb", valueFrom = "${var.app_secrets_arn}:ConnectionStrings__PaymentDb::" },
        { name = "ConnectionStrings__ProductDb", valueFrom = "${var.app_secrets_arn}:ConnectionStrings__ProductDb::" },
        { name = "ConnectionStrings__OrderDb", valueFrom = "${var.app_secrets_arn}:ConnectionStrings__OrderDb::" },
        { name = "ConnectionStrings__CartDb", valueFrom = "${var.app_secrets_arn}:ConnectionStrings__CartDb::" },
        { name = "ConnectionStrings__NotificationDb", valueFrom = "${var.app_secrets_arn}:ConnectionStrings__NotificationDb::" },
        { name = "ConnectionStrings__MediaDb", valueFrom = "${var.app_secrets_arn}:ConnectionStrings__MediaDb::" },
        { name = "ConnectionStrings__ShippingDb", valueFrom = "${var.app_secrets_arn}:ConnectionStrings__ShippingDb::" },
        { name = "Redis__URL", valueFrom = "${var.app_secrets_arn}:Redis__URL::" },
        { name = "Redis__Password", valueFrom = "${var.app_secrets_arn}:Redis__Password::" },
        { name = "Redis__Port", valueFrom = "${var.app_secrets_arn}:Redis__Port::" },
        { name = "Redis__Ssl", valueFrom = "${var.app_secrets_arn}:Redis__Ssl::" },
        { name = "Redis__AbortConnect", valueFrom = "${var.app_secrets_arn}:Redis__AbortConnect::" },
        { name = "Redis__User", valueFrom = "${var.app_secrets_arn}:Redis__User::" },
        { name = "MongoDB__Host", valueFrom = "${var.app_secrets_arn}:MongoDB__Host::" },
        { name = "MongoDB__User", valueFrom = "${var.app_secrets_arn}:MongoDB__User::" },
        { name = "MongoDB__Password", valueFrom = "${var.app_secrets_arn}:MongoDB__Password::" },
        { name = "MongoDB__AppName", valueFrom = "${var.app_secrets_arn}:MongoDB__AppName::" },
        # Các biến nhạy cảm khác
        { name = "Email__Smtp__Username", valueFrom = "${var.app_secrets_arn}:Email__Smtp__Username::" },
        { name = "Email__Smtp__Password", valueFrom = "${var.app_secrets_arn}:Email__Smtp__Password::" },
        { name = "Email__Smtp__Host", valueFrom = "${var.app_secrets_arn}:Email__Smtp__Host::" },
        { name = "SePay__Api", valueFrom = "${var.app_secrets_arn}:SePay__Api::" },
        { name = "SePay__Key", valueFrom = "${var.app_secrets_arn}:SePay__Key::" },
        { name = "Grafana__AdminPassword", valueFrom = "${var.app_secrets_arn}:Grafana__AdminPassword::" },
        { name = "Meilisearch__MasterKey", valueFrom = "${var.app_secrets_arn}:Meilisearch__MasterKey::" },
        { name = "GHN__Token", valueFrom = "${var.app_secrets_arn}:GHN__Token::" },
        { name = "GHN__ShopId", valueFrom = "${var.app_secrets_arn}:GHN__ShopId::" },
        { name = "GHN__ApiUrl", valueFrom = "${var.app_secrets_arn}:GHN__ApiUrl::" },


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
  

resource "aws_ecs_task_definition" "app" {
  family                   = "${var.project_name}-task"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = tostring(var.ecs_cpu)
  memory                   = tostring(var.ecs_memory)
  execution_role_arn       = aws_iam_role.app_role.arn
  task_role_arn            = aws_iam_role.app_role.arn
  container_definitions    = jsonencode(local.app_container_definitions)
  volume {
    name = "meili_data"
    efs_volume_configuration {
      file_system_id = aws_efs_file_system.meili_data.id
    }
  }
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
    container_name   = local.app_container_definitions[0].name
    container_port   = 8080
  }

  service_registries {
    registry_arn = aws_service_discovery_service.app.arn
  }

  depends_on = [aws_lb_listener.https]
}
