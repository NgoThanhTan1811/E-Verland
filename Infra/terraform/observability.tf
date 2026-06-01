# locals {
#   observability_namespace   = "${var.project_name}.local"
#   prometheus_container_name = "prometheus"
#   grafana_container_name    = "grafana"
#   loki_container_name       = "loki"

#   prometheus_config_b64   = base64encode(file("${path.module}/../docker/prometheus/prometheus.yml"))
#   grafana_datasources_b64 = base64encode(file("${path.module}/../docker/grafana/datasources.yaml"))
# }

# resource "aws_service_discovery_private_dns_namespace" "internal" {
#   name = local.observability_namespace
#   vpc  = module.vpc.vpc_id
# }

# resource "aws_service_discovery_service" "app" {
#   name = "app"

#   dns_config {
#     namespace_id   = aws_service_discovery_private_dns_namespace.internal.id
#     routing_policy = "MULTIVALUE"

#     dns_records {
#       ttl  = 10
#       type = "A"
#     }
#   }

#   health_check_custom_config {
#     failure_threshold = 1
#   }
# }

# resource "aws_service_discovery_service" "prometheus" {
#   name = "prometheus"

#   dns_config {
#     namespace_id   = aws_service_discovery_private_dns_namespace.internal.id
#     routing_policy = "MULTIVALUE"

#     dns_records {
#       ttl  = 10
#       type = "A"
#     }
#   }

#   health_check_custom_config {
#     failure_threshold = 1
#   }
# }

# resource "aws_service_discovery_service" "grafana" {
#   name = "grafana"

#   dns_config {
#     namespace_id   = aws_service_discovery_private_dns_namespace.internal.id
#     routing_policy = "MULTIVALUE"

#     dns_records {
#       ttl  = 10
#       type = "A"
#     }
#   }

#   health_check_custom_config {
#     failure_threshold = 1
#   }
# }

# resource "aws_service_discovery_service" "loki" {
#   name = "loki"

#   dns_config {
#     namespace_id   = aws_service_discovery_private_dns_namespace.internal.id
#     routing_policy = "MULTIVALUE"

#     dns_records {
#       ttl  = 10
#       type = "A"
#     }
#   }

#   health_check_custom_config {
#     failure_threshold = 1
#   }
# }

# resource "aws_cloudwatch_log_group" "prometheus" {
#   name              = "/ecs/${var.project_name}-prometheus"
#   retention_in_days = 14
# }

# resource "aws_cloudwatch_log_group" "grafana" {
#   name              = "/ecs/${var.project_name}-grafana"
#   retention_in_days = 14
# }

# resource "aws_cloudwatch_log_group" "loki" {
#   name              = "/ecs/${var.project_name}-loki"
#   retention_in_days = 14
# }

resource "aws_security_group" "observability_sg" {
  name        = "${var.project_name}-observability-sg"
  description = "Internal access to observability services"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port   = 3000
    to_port     = 3000
    protocol    = "tcp"
    cidr_blocks = [module.vpc.vpc_cidr_block]
  }

  ingress {
    from_port   = 9090
    to_port     = 9090
    protocol    = "tcp"
    cidr_blocks = [module.vpc.vpc_cidr_block]
  }

  ingress {
    from_port   = 3100
    to_port     = 3100
    protocol    = "tcp"
    cidr_blocks = [module.vpc.vpc_cidr_block]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "loki_efs_sg" {
  name        = "${var.project_name}-loki-efs-sg"
  description = "Allow Loki to reach EFS"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port       = 2049
    to_port         = 2049
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

# resource "aws_efs_file_system" "loki" {
#   encrypted = true
#   tags = {
#     Name = "${var.project_name}-loki"
#   }
# }

# resource "aws_efs_access_point" "loki" {
#   file_system_id = aws_efs_file_system.loki.id

#   posix_user {
#     uid = 1000
#     gid = 1000
#   }

#   root_directory {
#     path = "/loki"
#     creation_info {
#       owner_uid   = 1000
#       owner_gid   = 1000
#       permissions = "755"
#     }
#   }
# }

# # Tạo EFS File System
# resource "aws_efs_file_system" "meili_data" {
#   encrypted = true
#   tags = {
#     Name = "${var.project_name}-meili-efs"
#   }
# }

# # Tạo Mount Target cho mỗi subnet private
# resource "aws_efs_mount_target" "meilisearch" {
#   for_each       = { for idx, subnet_id in module.vpc.private_subnets : idx => subnet_id }
#   file_system_id = aws_efs_file_system.meili_data.id
#   subnet_id      = each.value
#   security_groups = [aws_security_group.meilisearch_efs_sg.id]
# }

# Security Group cho EFS (Cho phép traffic từ ECS Meilisearch vào port 2049)
resource "aws_security_group" "meilisearch_efs_sg" {
  name        = "${var.project_name}-meili-efs-sg"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port       = 2049
    to_port         = 2049
    protocol        = "tcp"
    security_groups = [aws_security_group.meilisearch_sg.id]
  }
}

# resource "aws_efs_mount_target" "loki" {
#   for_each = { for idx, subnet_id in module.vpc.private_subnets : idx => subnet_id }
#   file_system_id = aws_efs_file_system.loki.id
#   subnet_id      = each.value 
#   security_groups = [aws_security_group.loki_efs_sg.id]

# }

# # resource "aws_ecs_task_definition" "prometheus" {
# #   family                   = "${var.project_name}-prometheus"
# #   network_mode             = "awsvpc"
# #   requires_compatibilities = ["FARGATE"]
# #   cpu                      = "512"
# #   memory                   = "1024"
# #   execution_role_arn       = aws_iam_role.app_role.arn
# #   task_role_arn            = aws_iam_role.app_role.arn

# #   container_definitions = jsonencode([
# #     {
# #       name      = local.prometheus_container_name
# #       image     = "prom/prometheus:v2.51.0"
# #       essential = true
# #       portMappings = [
# #         {
# #           containerPort = 9090
# #           hostPort      = 9090
# #           protocol      = "tcp"
# #         }
# #       ]
# #       entryPoint = ["/bin/sh", "-c"]
# #       command = [
# #         "echo \"$PROM_CONFIG_BASE64\" | base64 -d > /etc/prometheus/prometheus.yml && /bin/prometheus --config.file=/etc/prometheus/prometheus.yml --storage.tsdb.path=/prometheus --storage.tsdb.retention.time=15d --web.enable-lifecycle"
# #       ]
# #       environment = [
# #         {
# #           name  = "PROM_CONFIG_BASE64"
# #           value = local.prometheus_config_b64
# #         }
# #       ]
# #       logConfiguration = {
# #         logDriver = "awslogs"
# #         options = {
# #           awslogs-group         = aws_cloudwatch_log_group.prometheus.name
# #           awslogs-region        = var.aws_region
# #           awslogs-stream-prefix = "ecs"
# #         }
# #       }
# #     }
# #   ])
# # }

# resource "aws_ecs_service" "prometheus" {
#   name            = "${var.project_name}-prometheus"
#   cluster         = aws_ecs_cluster.prod.id
#   task_definition = aws_ecs_task_definition.prometheus.arn
#   desired_count   = 0
#   launch_type     = "FARGATE"

#   network_configuration {
#     subnets          = module.vpc.private_subnets
#     security_groups  = [aws_security_group.observability_sg.id]
#     assign_public_ip = false
#   }

#   service_registries {
#     registry_arn = aws_service_discovery_service.prometheus.arn
#   }
# }

# # resource "aws_ecs_task_definition" "grafana" {
# #   family                   = "${var.project_name}-grafana"
# #   network_mode             = "awsvpc"
# #   requires_compatibilities = ["FARGATE"]
# #   cpu                      = "256"
# #   memory                   = "512"
# #   execution_role_arn       = aws_iam_role.app_role.arn
# #   task_role_arn            = aws_iam_role.app_role.arn

# #   container_definitions = jsonencode([
# #     {
# #       name      = local.grafana_container_name
# #       image     = "grafana/grafana:10.4.0"
# #       essential = true
# #       portMappings = [
# #         {
# #           containerPort = 3000
# #           hostPort      = 3000
# #           protocol      = "tcp"
# #         }
# #       ]
# #       entryPoint = ["/bin/sh", "-c"]
# #       command = [
# #         "mkdir -p /etc/grafana/provisioning/datasources && echo \"$GRAFANA_DATASOURCES_BASE64\" | base64 -d > /etc/grafana/provisioning/datasources/datasources.yaml && /run.sh"
# #       ]
# #       environment = [
# #         {
# #           name  = "GRAFANA_DATASOURCES_BASE64"
# #           value = local.grafana_datasources_b64
# #         },
# #         {
# #           name  = "GF_USERS_ALLOW_SIGN_UP"
# #           value = "false"
# #         },
# #         {
# #           name  = "GF_SERVER_ROOT_URL"
# #           value = "http://grafana.${var.project_name}.local:3000"
# #         }
# #       ]
# #       secrets = [
# #         {
# #           name      = "GF_SECURITY_ADMIN_PASSWORD"
# #           valueFrom = var.app_secrets_arn
# #         }
# #       ]
# #       logConfiguration = {
# #         logDriver = "awslogs"
# #         options = {
# #           awslogs-group         = aws_cloudwatch_log_group.grafana.name
# #           awslogs-region        = var.aws_region
# #           awslogs-stream-prefix = "ecs"
# #         }
# #       }
# #     }
# #   ])
# # }

# resource "aws_ecs_service" "grafana" {
#   name            = "${var.project_name}-grafana"
#   cluster         = aws_ecs_cluster.prod.id
#   task_definition = aws_ecs_task_definition.grafana.arn
#   desired_count   = 0
#   launch_type     = "FARGATE"

#   network_configuration {
#     subnets          = module.vpc.private_subnets
#     security_groups  = [aws_security_group.observability_sg.id]
#     assign_public_ip = false
#   }

#   service_registries {
#     registry_arn = aws_service_discovery_service.grafana.arn
#   }
# }

# # resource "aws_ecs_task_definition" "loki" {
# #   family                   = "${var.project_name}-loki"
# #   network_mode             = "awsvpc"
# #   requires_compatibilities = ["FARGATE"]
# #   cpu                      = "256"
# #   memory                   = "512"
# #   execution_role_arn       = aws_iam_role.app_role.arn
# #   task_role_arn            = aws_iam_role.app_role.arn

# #   volume {
# #     name = "loki-data"

# #     efs_volume_configuration {
# #       file_system_id     = aws_efs_file_system.loki.id
# #       transit_encryption = "ENABLED"

# #     authorization_config {
# #       access_point_id = "${aws_efs_access_point.loki.id}" 
# #       iam             = "ENABLED"
# #   }
# #     }
# #   }

# #   container_definitions = jsonencode([
# #     {
# #       name      = local.loki_container_name
# #       image     = "grafana/loki:3.0.0"
# #       essential = true
# #       portMappings = [
# #         {
# #           containerPort = 3100
# #           hostPort      = 3100
# #           protocol      = "tcp"
# #         }
# #       ]
# #       command = ["-config.file=/etc/loki/local-config.yaml"]
# #       mountPoints = [
# #         {
# #           sourceVolume  = "loki-data"
# #           containerPath = "/loki"
# #           readOnly      = false
# #         }
# #       ]
# #       logConfiguration = {
# #         logDriver = "awslogs"
# #         options = {
# #           awslogs-group         = aws_cloudwatch_log_group.loki.name
# #           awslogs-region        = var.aws_region
# #           awslogs-stream-prefix = "ecs"
# #         }
# #       }
# #     }
# #   ])
# # }

# resource "aws_ecs_service" "loki" {
#   name            = "${var.project_name}-loki"
#   cluster         = aws_ecs_cluster.prod.id
#   task_definition = aws_ecs_task_definition.loki.arn
#   desired_count   = 0
#   launch_type     = "FARGATE"

#   network_configuration {
#     subnets          = module.vpc.private_subnets
#     security_groups  = [aws_security_group.observability_sg.id]
#     assign_public_ip = false
#   }

#   service_registries {
#     registry_arn = aws_service_discovery_service.loki.arn
#   }
# }
