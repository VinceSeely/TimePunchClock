# Local values for dev environment
# These are computed values and naming conventions specific to dev

locals {
  # Environment configuration
  environment = "dev"
  location    = "eastus"

  # Naming convention: resource-type-app-environment
  resource_group_name    = "rg-timeclock-${local.environment}"
  sql_server_name        = "sql-timeclock-${local.environment}-${random_string.unique_suffix.result}"
  sql_database_name      = "timeclockdb"
  sql_admin_username     = "sqladmin"
  acr_name               = "acrtimeclock${local.environment}${random_string.unique_suffix.result}"
  backend_container_name = "backend-api-${local.environment}"
  backend_dns_label      = "timeclock-api-${local.environment}-${random_string.unique_suffix.result}"
  backend_image_name     = "backend-api"
  backend_image_tag      = "latest"
  static_web_app_name    = "swa-timeclock-${local.environment}"

  # Common tags
  tags = {
    environment = local.environment
    project     = "timeclock"
    managed_by  = "terraform"
  }
}

# Generate a unique suffix for globally unique names
resource "random_string" "unique_suffix" {
  length  = 6
  special = false
  upper   = false
}
