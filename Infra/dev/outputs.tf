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

output "acr_login_server" {
  description = "Login server of the Azure Container Registry"
  value       = azurerm_container_registry.main.login_server
}

output "acr_admin_username" {
  description = "Admin username for the Azure Container Registry"
  value       = azurerm_container_registry.main.admin_username
  sensitive   = true
}

output "acr_admin_password" {
  description = "Admin password for the Azure Container Registry"
  value       = azurerm_container_registry.main.admin_password
  sensitive   = true
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
  value       = "http://${azurerm_container_group.backend.ip_address}"
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
  value       = "api://timeclock-api-${local.environment}"
}

output "azuread_api_scope" {
  description = "Azure AD API Scope"
  value       = "api://timeclock-api-${local.environment}/access_as_user"
}

output "azuread_blazor_application_id" {
  description = "Azure AD Blazor Application (Client) ID"
  value       = azuread_application.blazor.client_id
}

output "azuread_configuration_summary" {
  description = "Azure AD Configuration Summary for easy reference"
  value = <<-EOT

  Azure AD Configuration:
  ----------------------
  Tenant ID: ${data.azurerm_client_config.current.tenant_id}

  API App (Backend):
    Application ID: ${azuread_application.api.client_id}
    Identifier URI: api://timeclock-api-${local.environment}
    Scope: api://timeclock-api-${local.environment}/access_as_user

  Blazor App (Frontend):
    Application ID: ${azuread_application.blazor.client_id}
    Redirect URIs:
      - http://localhost:5000/authentication/login-callback
      - https://${azurerm_static_web_app.blazor.default_host_name}/authentication/login-callback

  All configuration values are also stored in Key Vault: ${azurerm_key_vault.kv.name}
  EOT
}