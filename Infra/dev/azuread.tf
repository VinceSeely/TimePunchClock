# Azure AD App Registrations for Authentication

# API App Registration (Backend)
resource "azuread_application" "api" {
  display_name = "TimeClock API - ${local.environment}"
  owners       = [data.azurerm_client_config.current.object_id]

  # Configure API to accept access tokens
  api {
    # Define the oauth2 permission scope that the API exposes
    oauth2_permission_scope {
      admin_consent_description  = "Allow the application to access the TimeClock API on behalf of the signed-in user"
      admin_consent_display_name = "Access TimeClock API as user"
      enabled                    = true
      id                         = random_uuid.api_scope_id.result
      type                       = "User"
      user_consent_description   = "Allow the application to access the TimeClock API on your behalf"
      user_consent_display_name  = "Access TimeClock API"
      value                      = "access_as_user"
    }
  }

  # Configure single-tenant authentication
  sign_in_audience = "AzureADMyOrg"

  # Configure web API settings
  web {
    implicit_grant {
      access_token_issuance_enabled = true
      id_token_issuance_enabled     = true
    }
  }

  tags = [
    "environment:${local.environment}",
    "managed-by:terraform"
  ]
}

# Generate a random UUID for the API scope
resource "random_uuid" "api_scope_id" {}

# Set the identifier URI for the API app
# This must be done separately to avoid circular reference
resource "azuread_application_identifier_uri" "api" {
  application_id = azuread_application.api.id
  identifier_uri = "api://${azuread_application.api.client_id}"
}

# Create a service principal for the API app
resource "azuread_service_principal" "api" {
  client_id                    = azuread_application.api.client_id
  app_role_assignment_required = false
  owners                       = [data.azurerm_client_config.current.object_id]

  tags = [
    "environment:${local.environment}",
    "managed-by:terraform"
  ]
}

# Blazor App Registration (Frontend)
resource "azuread_application" "blazor" {
  display_name = "TimeClock Blazor - ${local.environment}"
  owners       = [data.azurerm_client_config.current.object_id]

  # Configure single-tenant authentication
  sign_in_audience = "AzureADMyOrg"

  # Configure SPA redirect URIs
  single_page_application {
    redirect_uris = [
      "http://localhost:5000/authentication/login-callback",
      "${azurerm_static_web_app.blazor.default_host_name}/authentication/login-callback",
      "https://${azurerm_static_web_app.blazor.default_host_name}/authentication/login-callback"
    ]
  }

  # Enable implicit grant flow for SPA
  web {
    implicit_grant {
      access_token_issuance_enabled = true
      id_token_issuance_enabled     = true
    }
  }

  # Request API permissions to call the backend API
  required_resource_access {
    # Reference to the API app we created above
    resource_app_id = azuread_application.api.client_id

    # Request the access_as_user scope
    resource_access {
      id   = random_uuid.api_scope_id.result
      type = "Scope"
    }
  }

  # Also request Microsoft Graph permissions for user profile
  required_resource_access {
    resource_app_id = "00000003-0000-0000-c000-000000000000" # Microsoft Graph

    resource_access {
      id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d" # User.Read
      type = "Scope"
    }
  }

  tags = [
    "environment:${local.environment}",
    "managed-by:terraform"
  ]
}

# Create a service principal for the Blazor app
resource "azuread_service_principal" "blazor" {
  client_id                    = azuread_application.blazor.client_id
  app_role_assignment_required = false
  owners                       = [data.azurerm_client_config.current.object_id]

  tags = [
    "environment:${local.environment}",
    "managed-by:terraform"
  ]
}

# Grant admin consent for the Blazor app to access the API
# Note: This requires the service principal running Terraform to have permissions to grant consent
resource "azuread_service_principal_delegated_permission_grant" "blazor_to_api" {
  service_principal_object_id          = azuread_service_principal.blazor.object_id
  resource_service_principal_object_id = azuread_service_principal.api.object_id
  claim_values                         = ["access_as_user"]
}

# Store Azure AD configuration in Key Vault
resource "azurerm_key_vault_secret" "azuread_tenant_id" {
  name         = "azuread-tenant-id"
  value        = data.azurerm_client_config.current.tenant_id
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "azuread_api_client_id" {
  name         = "azuread-api-client-id"
  value        = azuread_application.api.client_id
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "azuread_blazor_client_id" {
  name         = "azuread-blazor-client-id"
  value        = azuread_application.blazor.client_id
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "azuread_api_scope" {
  name         = "azuread-api-scope"
  value        = "api://${azuread_application.api.client_id}/access_as_user"
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "azuread_api_identifier_uri" {
  name         = "azuread-api-identifier-uri"
  value        = "api://${azuread_application.api.client_id}"
  key_vault_id = azurerm_key_vault.kv.id
}
