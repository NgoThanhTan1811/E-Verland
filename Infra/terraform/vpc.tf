module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "5.0.0"

  name = "everland-vpc"
  cidr = "10.0.0.0/16"
  azs  = ["ap-southeast-1a", "ap-southeast-1b"]

  public_subnets  = ["10.0.1.0/24", "10.0.2.0/24"]   # Cho ALB & EC2 Tooling
  private_subnets = ["10.0.101.0/24", "10.0.102.0/24"] # Cho ECS App

  enable_nat_gateway = true
  single_nat_gateway = true # Tiết kiệm chi phí
}