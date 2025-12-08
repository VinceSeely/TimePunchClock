# Local values for dev environment
# These are computed values and naming conventions specific to dev

locals {
  # Environment configuration
  environment = "dev"
  location    = "eastus2" # Changed from eastus due to SQL provisioning restrictions

  # Naming convention: resource-type-app-environment
  resource_group_name = "rg-timeclock-${local.environment}"
  sql_server_name     = "sql-timeclock-${local.environment}-${random_string.unique_suffix.result}"
  sql_database_name   = "timeclockdb"
  sql_admin_username  = "sqladmin"
  static_web_app_name = "swa-timeclock-${local.environment}"

  # Container Apps naming (replaces Container Instance)
  container_app_env_name = "cae-timeclock-${local.environment}"
  container_app_name     = "ca-backend-${local.environment}"

  # GitHub Container Registry configuration
  ghcr_registry      = "ghcr.io"
  github_org         = "vinceseely"
  backend_image_name = "${local.github_org}/timepunchclock/backend-api"
  backend_image_tag  = "latest"

  # Common tags with FinOps best practices
  tags = {
    # Environment & Management
    environment = local.environment
    project     = var.application_name
    managed_by  = "terraform"

    # FinOps Tags
    cost_center      = var.cost_center
    owner            = var.owner != "" ? var.owner : "unassigned"
    budget_code      = var.budget_code
    application_name = var.application_name

    # Cost Optimization
    auto_shutdown = "true" # Indicator for cost optimization policies
    backup_policy = "standard"

    # Deployment Info
    deployed_by = "github-actions"
    # Note: timestamp() removed to prevent constant drift on every apply
  }
}

# Generate a unique suffix for globally unique names
resource "random_string" "unique_suffix" {
  length  = 6
  special = false
  upper   = false
}
