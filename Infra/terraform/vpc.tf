module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "6.0.1"

  name = "e-verland-vpc"
  cidr = "10.0.0.0/16"
  azs  = ["ap-southeast-1a", "ap-southeast-1b"]

  public_subnets  = ["10.0.1.0/24", "10.0.2.0/24"]     # Cho ALB & EC2 Tooling
  private_subnets = ["10.0.101.0/24", "10.0.102.0/24"] # Cho ECS App

  enable_nat_gateway = true
  single_nat_gateway = true # Tiết kiệm chi phí

  tags = {
    Name = "e-verland-vpc"
  }
}

module "vpc_endpoints" {
  source = "terraform-aws-modules/vpc/aws//modules/vpc-endpoints"

  vpc_id = module.vpc.vpc_id

  create_security_group      = true
  security_group_name        = "${var.project_name}-vpc-endpoints-sg"
  security_group_description = "Security group for VPC interface endpoints"
  security_group_rules = {
    ingress_https = {
      description = "HTTPS from VPC"
      cidr_blocks = [module.vpc.vpc_cidr_block]
    }
  }

  endpoints = {
    s3 = {
      service         = "s3"
      service_type    = "Gateway"
      route_table_ids = module.vpc.private_route_table_ids
      tags = {
        Name = "${var.project_name}-s3-endpoint"
      }
    }

    secretsmanager = {
      service             = "secretsmanager"
      private_dns_enabled = true
      subnet_ids          = module.vpc.private_subnets
      tags = {
        Name = "${var.project_name}-secretsmanager-endpoint"
      }
    }

    sqs = {
      service             = "sqs"
      private_dns_enabled = true
      subnet_ids          = module.vpc.private_subnets
      tags = {
        Name = "${var.project_name}-sqs-endpoint"
      }
    }

    sns = {
      service             = "sns"
      private_dns_enabled = true
      subnet_ids          = module.vpc.private_subnets
      tags = {
        Name = "${var.project_name}-sns-endpoint"
      }
    }
  }
}