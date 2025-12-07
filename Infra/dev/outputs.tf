output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "key_vault_name" {
  description = "Name of the Key Vault"
  value       = azurerm_key_vault.kv.name
}

output "key_vault_uri" {
  description = "URI of the Key Vault"
  value       = azurerm_key_vault.kv.vault_uri
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of the SQL Server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_database_name" {
  description = "Name of the SQL Database"
  value       = azurerm_mssql_database.main.name
}

output "sql_connection_string" {
  description = "SQL Database connection string (stored in Key Vault as 'sql-connection-string')"
  value       = "Retrieve from Key Vault: ${azurerm_key_vault.kv.name}/secrets/sql-connection-string"
  sensitive   = false
}

output "backend_fqdn" {
  description = "FQDN of the backend container (only if DNS label was set)"
  value       = azurerm_container_group.backend.fqdn
}

output "backend_ip_address" {
  description = "Public IP address of the backend container - use this to access your API"
  value       = azurerm_container_group.backend.ip_address
}

output "backend_url" {
  description = "Backend API URL - use this in your Blazor app configuration"
  value       = azurerm_container_group.backend.fqdn != null && azurerm_container_group.backend.fqdn != "" ? "http://${azurerm_container_group.backend.fqdn}" : "http://${azurerm_container_group.backend.ip_address}"
}

output "static_web_app_url" {
  description = "Default hostname of the Static Web App"
  value       = "https://${azurerm_static_web_app.blazor.default_host_name}"
}

output "static_web_app_api_key" {
  description = "API key for deploying to Static Web App"
  value       = azurerm_static_web_app.blazor.api_key
  sensitive   = true
}

# Azure AD Outputs
output "azuread_tenant_id" {
  description = "Azure AD Tenant ID"
  value       = data.azurerm_client_config.current.tenant_id
}

output "azuread_api_application_id" {
  description = "Azure AD API Application (Client) ID"
  value       = azuread_application.api.client_id
}

output "azuread_api_identifier_uri" {
  description = "Azure AD API Identifier URI"
  value       = "api://${azuread_application.api.client_id}"
}

output "azuread_api_scope" {
  description = "Azure AD API Scope"
  value       = "api://${azuread_application.api.client_id}/access_as_user"
}

output "azuread_blazor_application_id" {
  description = "Azure AD Blazor Application (Client) ID"
  value       = azuread_application.blazor.client_id
}

output "azuread_configuration_summary" {
  description = "Azure AD Configuration Summary for easy reference"
  value       = <<-EOT

  Azure AD Configuration:
  ----------------------
  Tenant ID: ${data.azurerm_client_config.current.tenant_id}

  API App (Backend):
    Application ID: ${azuread_application.api.client_id}
    Identifier URI: api://${azuread_application.api.client_id}
    Scope: api://${azuread_application.api.client_id}/access_as_user

  Blazor App (Frontend):
    Application ID: ${azuread_application.blazor.client_id}
    Redirect URIs:
      - http://localhost:5000/authentication/login-callback
      - https://${azurerm_static_web_app.blazor.default_host_name}/authentication/login-callback

  CORS Configuration:
    Allowed Origins: ${join(", ", var.cors_allowed_origins)}
    To update: Add -var="cors_allowed_origins=[\"https://yourdomain.com\"]" to terraform apply

  All configuration values are also stored in Key Vault: ${azurerm_key_vault.kv.name}
  EOT
}

# FinOps Outputs
output "finops_tags" {
  description = "FinOps tags applied to all resources for cost tracking"
  value = {
    environment      = local.environment
    cost_center      = var.cost_center
    owner            = var.owner
    budget_code      = var.budget_code
    application_name = var.application_name
  }
}

output "resource_summary" {
  description = "Summary of deployed resources for cost analysis"
  value = {
    resource_group         = azurerm_resource_group.main.name
    location               = azurerm_resource_group.main.location
    sql_server_sku         = "Basic"
    static_web_app_sku     = "Free"
    container_cpu          = "0.5"
    container_memory       = "1.0"
    estimated_monthly_cost = "~$5-10 USD (SQL Basic ~$5 + Container Instance ~$5)"
  }
}