terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.47"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy    = true
      recover_soft_deleted_key_vaults = true
    }
  }
  use_oidc = true
}

provider "azuread" {
  # Uses OIDC authentication for GitHub Actions
  use_oidc = true
}

# Get current Azure client configuration
data "azurerm_client_config" "current" {}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = local.resource_group_name
  location = local.location
  tags     = local.tags
}

# Azure Key Vault for secrets management
resource "azurerm_key_vault" "kv" {
  name                       = "kv-timeclock-${local.environment}"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled   = false

  enable_rbac_authorization = false

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get",
      "List",
      "Set",
      "Delete",
      "Purge",
      "Recover"
    ]
  }

  tags = local.tags
}

# Generate random password for SQL Server
resource "random_password" "sql_admin_password" {
  length  = 24
  special = true
  upper   = true
  lower   = true
  numeric = true
}

# Store SQL admin password in Key Vault
resource "azurerm_key_vault_secret" "sql_password" {
  name         = "sql-admin-password"
  value        = random_password.sql_admin_password.result
  key_vault_id = azurerm_key_vault.kv.id
}

# Store SQL connection string in Key Vault
resource "azurerm_key_vault_secret" "sql_connection_string" {
  name         = "sql-connection-string"
  value        = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${random_password.sql_admin_password.result};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = azurerm_key_vault.kv.id

  depends_on = [azurerm_mssql_database.main]
}

# SQL Server
resource "azurerm_mssql_server" "main" {
  name                         = var.sql_server_name != "" ? var.sql_server_name : local.sql_server_name
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = random_password.sql_admin_password.result
  minimum_tls_version          = "1.2"

  dynamic "azuread_administrator" {
    for_each = var.azuread_admin_object_id != "" ? [1] : []
    content {
      login_username = var.azuread_admin_login
      object_id      = var.azuread_admin_object_id
    }
  }

  tags = local.tags
}

# SQL Database (Basic Tier)
resource "azurerm_mssql_database" "main" {
  name           = local.sql_database_name
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  max_size_gb    = 2 # Basic tier supports up to 2GB
  sku_name       = "Basic"
  zone_redundant = false

  # Note: Basic tier does not support configurable backup storage redundancy
  # It automatically uses Local (LRS) redundancy which is the most cost-effective option

  tags = local.tags
}

# Firewall rule to allow Azure services
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Optional: Firewall rule for your IP (update with your IP)
resource "azurerm_mssql_firewall_rule" "allow_my_ip" {
  name             = "AllowMyIP"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = var.my_ip_address
  end_ip_address   = var.my_ip_address
}

# Container Apps Environment (required for Container Apps)
resource "azurerm_container_app_environment" "main" {
  name                       = "${local.backend_container_name}-env"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id

  tags = local.tags
}

# Log Analytics Workspace for Container Apps
resource "azurerm_log_analytics_workspace" "main" {
  name                = "${local.backend_container_name}-logs"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = local.tags
}

# Container App for Backend (replaces Container Instance)
resource "azurerm_container_app" "backend" {
  name                         = local.backend_container_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    container {
      name   = "backend"
      image  = "${local.ghcr_registry}/${local.backend_image_name}:${local.backend_image_tag}"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = local.environment
      }

      env {
        name  = "Authentication__Enabled"
        value = "true"
      }

      # Add CORS allowed origins
      dynamic "env" {
        for_each = length(var.cors_allowed_origins) > 0 ? { for idx, origin in var.cors_allowed_origins : idx => origin } : {}
        content {
          name  = "Cors__AllowedOrigins__${env.key}"
          value = env.value
        }
      }

      # Secure environment variable for connection string
      env {
        name        = "ConnectionStrings__DefaultConnection"
        secret_name = "sql-connection-string"
      }
    }

    min_replicas = 1
    max_replicas = 2
  }

  # Define secret for SQL connection string
  secret {
    name  = "sql-connection-string"
    value = azurerm_key_vault_secret.sql_connection_string.value
  }

  ingress {
    external_enabled = true
    target_port      = 80
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  # Note: Container Apps supports registry authentication
  # For private GitHub Container Registry, add:
  # registry {
  #   server               = "ghcr.io"
  #   username             = var.github_username
  #   password_secret_name = "registry-password"
  # }

  tags = local.tags
}

# Static Web App for Blazor SPA
resource "azurerm_static_web_app" "blazor" {
  name                = local.static_web_app_name
  resource_group_name = azurerm_resource_group.main.name
  location            = "eastus2" # Static Web Apps have limited region availability
  sku_tier            = "Free"
  sku_size            = "Free"

  tags = local.tags
}