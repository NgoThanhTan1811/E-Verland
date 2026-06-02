# locals {
#   meilisearch_container_name = "meilisearch"
# }

# resource "aws_service_discovery_service" "meilisearch" {
#   name = "meilisearch"

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

# resource "aws_cloudwatch_log_group" "meilisearch" {
#   name              = "/ecs/${var.project_name}-meilisearch"
#   retention_in_days = 14
# }

resource "aws_security_group" "meilisearch_sg" {
  name        = "${var.project_name}-meilisearch-sg"
  description = "Allow internal access to Meilisearch"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port       = 7700
    to_port         = 7700
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs_sg.id]
  }

  ingress {
    from_port       = 7700
    to_port         = 7700
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

# resource "aws_ecs_task_definition" "meilisearch" {
#   family                   = "${var.project_name}-meilisearch"
#   network_mode             = "awsvpc"
#   requires_compatibilities = ["FARGATE"]
#   cpu                      = "512"
#   memory                   = "1024"
#   execution_role_arn       = aws_iam_role.app_role.arn
#   task_role_arn            = aws_iam_role.app_role.arn

#   # 1. Định nghĩa Volume ở cấp độ Task (ngang hàng với container_definitions)
#   volume {
#     name = "meili_data"
#     efs_volume_configuration {
#       file_system_id = aws_efs_file_system.meili_data.id
#     }
#   }

#   # 2. Định nghĩa container_definitions
#   container_definitions = jsonencode([
#     {
#       name      = local.meilisearch_container_name
#       image     = "getmeili/meilisearch:v1.7"
#       essential = true
#       portMappings = [
#         {
#           containerPort = 7700
#           hostPort      = 7700
#           protocol      = "tcp"
#         }
#       ]
#       # 3. Mount volume vào container tại đây
#       mountPoints = [
#         {
#           sourceVolume  = "meili_data"
#           containerPath = "/meili_data" 
#           readOnly      = false
#         }
#       ]
#       environment = [
#         { name = "MEILI_ENV", value = "production" },
#         { name = "MEILI_NO_ANALYTICS", value = "true" },
#         # BẮT BUỘC: Bảo Meilisearch lưu data vào path đã mount
#         { name = "MEILI_DB_PATH", value = "/meili_data/data.ms" } 
#       ]
#       secrets = [
#         {
#           name      = "Meilisearch__MasterKey"
#           valueFrom = var.app_secrets_arn
#         }
#       ]
#       logConfiguration = {
#         logDriver = "awslogs"
#         options = {
#           awslogs-group         = aws_cloudwatch_log_group.meilisearch.name
#           awslogs-region        = var.aws_region
#           awslogs-stream-prefix = "ecs"
#         }
#       }
#     }
#   ])
# }


# resource "aws_ecs_service" "meilisearch" {
#   name            = "${var.project_name}-meilisearch"
#   cluster         = aws_ecs_cluster.prod.id
#   task_definition = aws_ecs_task_definition.meilisearch.arn
#   desired_count   = 0
#   launch_type     = "FARGATE"

#   network_configuration {
#     subnets          = module.vpc.private_subnets
#     security_groups  = [aws_security_group.meilisearch_sg.id]
#     assign_public_ip = false
#   }

#   service_registries {
#     registry_arn = aws_service_discovery_service.meilisearch.arn
#   }
# }
