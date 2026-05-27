# Terraform Configuration Review & Fixes

## Changes Made

### 1. **providers.tf** ✅ Fixed

- **Consolidated duplicate terraform blocks** (was: 2 blocks, now: 1 block)
- **Updated Cloudflare provider version** from ~> 4.0 to ~> 5.0
- **Fixed provider authentication** to use variables instead of hardcoded values:
  - `provider "aws"` now uses `region = var.aws_region`
  - `provider "cloudflare"` now uses `api_token = var.cloudflare_api_token`
- Kept S3 backend configuration intact

### 2. **variables.tf** ✅ Cleaned & Enhanced

- **Removed all duplicate variable definitions**
- **Organized into logical sections**:
  - AWS & Cloud Infrastructure
  - Application URLs (Production)
  - Storage Configuration
  - Database & Security Secrets
  - ECS/Fargate Configuration
- **Updated production defaults**:
  - `backend_url` = https://api.e-verland.site
  - `frontend_url` = https://e-verland.site
  - `s3_bucket_name` = e-verland-media
  - `s3_region` = ap-southeast-1 (fixed from "auto")
  - `desired_count` = 2 (for HA)
  - `ecs_cpu` = 1024, `ecs_memory` = 2048
- **Improved descriptions** for all variables

### 3. **compute.tf** ✅ Fixed

- Changed hardcoded domain in Cloudflare data source:

  ```hcl
  # Before
  data "cloudflare_zone" "main" {
    name = "e-verland.site"
  }

  # After
  data "cloudflare_zone" "main" {
    name = var.domain_name
  }
  ```

### 4. **terraform.tfvars.example** ✅ Created

- New file as reference template for required variables
- Includes placeholders for:
  - Cloudflare API token (sensitive)
  - AWS Secrets Manager ARNs (secrets)
  - ECR image URI
  - S3 bucket configuration
  - ECS/Fargate sizing

## Architecture Summary

```
Internet → Cloudflare (api.e-verland.site, e-verland.site)
        ↓
        ALB (Security Group: Only Cloudflare IPs)
        ↓
        ECS Fargate (Private Subnet)
        ↓
        RDS / ElastiCache / S3
```

- **Frontend:** https://e-verland.site
- **Backend API:** https://api.e-verland.site
- **Storage:** S3 bucket (e-verland-media) or Cloudflare R2
- **High Availability:** 2+ Fargate tasks across AZs

## Next Steps

### 1. **Prepare terraform.tfvars**

```bash
cp Infra/terraform/terraform.tfvars.example Infra/terraform/terraform.tfvars
# Edit terraform.tfvars with your actual values:
# - AWS Account ID
# - Cloudflare API Token
# - ECR image URI
# - RDS connection string ARN
# - JWT secret ARN
# - S3 credentials ARNs
```

### 2. **Initialize Terraform**

```bash
cd Infra/terraform
terraform init
```

(This will initialize the S3 backend and download modules)

### 3. **Validate Configuration**

```bash
terraform validate
terraform fmt -recursive  # Auto-format
```

### 4. **Plan & Review**

```bash
terraform plan -var-file=terraform.tfvars -out=tfplan
# Review the plan output for correctness
```

### 5. **Apply (when ready)**

```bash
terraform apply tfplan
```

## Security Notes

1. **Never commit terraform.tfvars** – Add to .gitignore
2. **Use AWS Secrets Manager** for sensitive values (DB password, JWT key, S3 credentials)
3. **Cloudflare API token** – Use environment variable in CI/CD:
   ```bash
   export TF_VAR_cloudflare_api_token="your_token"
   ```
4. **ALB Security Group** – Restricted to Cloudflare IPs only (computed from data source)
5. **ECS Task Role** – Minimal IAM permissions for S3, Secrets Manager, CloudWatch

## Files Modified

- [Infra/terraform/providers.tf](../providers.tf) – Provider configuration
- [Infra/terraform/variables.tf](../variables.tf) – All variables (deduplicated)
- [Infra/terraform/compute.tf](../compute.tf) – Cloudflare domain now uses variable
- **NEW:** [Infra/terraform/terraform.tfvars.example](../terraform.tfvars.example) – Reference template

---

**Status:** ✅ Ready for `terraform init` and planning
