# Azure Container Apps Configuration
# This replaces the Azure Container Instance with a more cost-effective and scalable solution

# Container App Environment
# This is the hosting environment for Container Apps (similar to an App Service Plan)
resource "azurerm_container_app_environment" "main" {
  name                = "cae-timeclock-${local.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tags = local.tags
}

# Log Analytics Workspace for Container Apps
# Required for Container App Environment monitoring and logging
resource "azurerm_log_analytics_workspace" "container_apps" {
  name                = "law-timeclock-${local.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = local.tags
}

# Update Container App Environment with Log Analytics
resource "azurerm_container_app_environment" "main_with_logs" {
  name                       = "cae-timeclock-${local.environment}"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.container_apps.id

  tags = local.tags
}

# Backend API Container App
resource "azurerm_container_app" "backend" {
  name                         = "ca-backend-${local.environment}"
  container_app_environment_id = azurerm_container_app_environment.main_with_logs.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  # Identity for accessing Key Vault and other Azure resources
  identity {
    type = "SystemAssigned"
  }

  template {
    # Scaling configuration - can scale to zero for cost savings
    min_replicas = 0 # Scale to zero when idle (saves costs)
    max_replicas = 3 # Scale up to 3 instances if needed

    container {
      name   = "backend-api"
      image  = "${local.ghcr_registry}/${local.backend_image_name}:${local.backend_image_tag}"
      cpu    = 0.5
      memory = "1Gi"

      # Environment variables (non-sensitive)
      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = local.environment == "dev" ? "Development" : "Production"
      }

      env {
        name  = "Authentication__Enabled"
        value = "true"
      }

      # Azure AD Configuration
      env {
        name  = "AzureAd__TenantId"
        value = data.azurerm_client_config.current.tenant_id
      }

      env {
        name  = "AzureAd__ClientId"
        value = azuread_application.api.client_id
      }

      env {
        name  = "AzureAd__Audience"
        value = "api://${azuread_application.api.client_id}"
      }

      env {
        name  = "AzureAd__Authority"
        value = "https://login.microsoftonline.com/${data.azurerm_client_config.current.tenant_id}"
      }

      # CORS Configuration - dynamically add allowed origins
      dynamic "env" {
        for_each = length(var.cors_allowed_origins) > 0 ? { for idx, origin in var.cors_allowed_origins : idx => origin } : {}
        content {
          name  = "Cors__AllowedOrigins__${env.key}"
          value = env.value
        }
      }

      # Sensitive environment variables (secrets)
      env {
        name        = "ConnectionStrings__DefaultConnection"
        secret_name = "sql-connection-string"
      }
    }
  }

  # Secrets configuration
  secret {
    name  = "sql-connection-string"
    value = azurerm_key_vault_secret.sql_connection_string.value
  }

  # Ingress configuration - enables HTTPS and external access
  ingress {
    external_enabled = true
    target_port      = 80

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }

    # CORS settings for ingress
    allow_insecure_connections = false # Force HTTPS only

    # Additional transport settings
    transport = "auto" # HTTP/1.1 and HTTP/2
  }

  tags = local.tags

  # Ensure Key Vault secret exists before creating Container App
  depends_on = [
    azurerm_key_vault_secret.sql_connection_string
  ]
}

# Grant Container App access to Key Vault (using System Assigned Managed Identity)
resource "azurerm_key_vault_access_policy" "container_app" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_container_app.backend.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]

  depends_on = [azurerm_container_app.backend]
}
