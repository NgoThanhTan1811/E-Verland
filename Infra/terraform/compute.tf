resource "aws_security_group" "alb_sg" {
  name        = "${var.project_name}-alb-sg"
  description = "Allow traffic to the ALB"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = var.cloudflare_ingress_cidrs
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = var.cloudflare_ingress_cidrs
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
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

resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.main.arn
  port              = 80
  protocol          = "HTTP"

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
  app_container_name = "everland-app"
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
        { name = "Storage__Provider", value = var.storage_provider },
        { name = "Storage__BaseUrl", value = var.storage_base_url },
        { name = "AWS__S3__BucketName", value = var.s3_bucket_name },
        { name = "AWS__S3__Region", value = var.s3_region },
        { name = "AWS__S3__BaseUrl", value = var.storage_base_url },
        { name = "AWS__S3__ServiceUrl", value = var.s3_service_url },
        { name = "AWS__S3__ForcePathStyle", value = var.s3_force_path_style ? "true" : "false" }
      ]
      secrets = concat(
        var.db_connection_secret_arn != "" ? [{ name = "ConnectionStrings__DefaultConnection", valueFrom = var.db_connection_secret_arn }] : [],
        var.jwt_key_secret_arn != "" ? [{ name = "Jwt__Key", valueFrom = var.jwt_key_secret_arn }] : [],
        var.s3_access_key_secret_arn != "" ? [{ name = "AWS__S3__AccessKey", valueFrom = var.s3_access_key_secret_arn }] : [],
        var.s3_secret_key_secret_arn != "" ? [{ name = "AWS__S3__SecretKey", valueFrom = var.s3_secret_key_secret_arn }] : []
      )
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

  depends_on = [aws_lb_listener.http]
}