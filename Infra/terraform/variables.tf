// AWS & Cloud Infrastructure
variable "project_name" {
  description = "Project prefix used across AWS resources."
  type        = string
  default     = "e-verland"
}

variable "aws_region" {
  description = "AWS region for the stack."
  type        = string
  default     = "ap-southeast-1"
}

variable "domain_name" {
  description = "Public domain (Cloudflare zone name)."
  type        = string
  default     = "e-verland.site"
}

variable "cloudflare_api_token" {
  description = "Cloudflare API token with DNS edit permissions."
  type        = string
  default     = ""
}

// Application URLs (Production)
variable "backend_url" {
  description = "Public backend URL used by the app."
  type        = string
  default     = "https://api.e-verland.site"
}

variable "frontend_url" {
  description = "Frontend URL used by auth callbacks and CORS."
  type        = string
  default     = "https://e-verland.site"
}

// Storage Configuration
variable "storage_provider" {
  description = "Storage provider for media files. Use S3 for R2-compatible storage."
  type        = string
  default     = "S3"
}

variable "r2_account_id" {
  description = "Cloudflare R2 account ID for building S3-compatible endpoint URL."
  type        = string
  default     = ""
}

variable "storage_base_url" {
  description = "Public base URL for stored media. e.g. https://<bucket>.s3.amazonaws.com or Cloudflare R2 URL."
  type        = string
  default     = ""
}

variable "s3_bucket_name" {
  description = "S3 bucket name used by the media storage service."
  type        = string
  default     = "e-verland-media"
}

variable "s3_region" {
  description = "AWS region for S3 bucket. For Cloudflare R2, use 'auto'."
  type        = string
  default     = "ap-southeast-1"
}

variable "s3_service_url" {
  description = "S3-compatible endpoint URL (e.g. Cloudflare R2). Leave empty for AWS S3."
  type        = string
  default     = ""
}

variable "s3_force_path_style" {
  description = "Whether to force path-style addressing for the S3 client. Set to true for R2."
  type        = bool
  default     = true
}

// ECS/Fargate Configuration
variable "container_image" {
  description = "Full ECR image URI for the backend container (e.g. <account>.dkr.ecr.ap-southeast-1.amazonaws.com/e-verland-backend:latest)."
  type        = string
  default     = ""
}

variable "desired_count" {
  description = "Desired number of backend Fargate tasks (2+ for high availability)."
  type        = number
  default     = 1
}

variable "ecs_cpu" {
  description = "CPU units for each Fargate task (256, 512, 1024, 2048, 4096)."
  type        = number
  default     = 1024
}

variable "ecs_memory" {
  description = "Memory in MiB for each Fargate task."
  type        = number
  default     = 2048
}

# ─── HTTPS / ACM ──────────────────────────────────────────────────────────────

variable "acm_certificate_arn" {
  description = "ARN of the ACM certificate for the HTTPS ALB listener."
  type        = string
  default     = ""
}

// AWS Secrets Manager ARNs
variable "app_secrets_arn" {
  description = "ARN of the AWS Secrets Manager secret containing application secrets."
  type        = string
}

variable "zone_id" {
  description = "Cloudflare Zone ID for the domain. Used to manage DNS records."
  type        = string
  default     = ""
}