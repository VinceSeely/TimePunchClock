# Azure AD App Registrations Setup

This document describes how to configure Azure AD app registrations for the TimeClock application authentication.

## Overview

The TimeClock application requires two Azure AD app registrations:
1. **API App Registration** - For the backend API (TimeApi)
2. **Blazor App Registration** - For the frontend Blazor WASM app (TimeClockUI)

## Prerequisites

- Azure subscription with appropriate permissions
- Azure CLI installed (`az --version` to verify)
- Logged in to Azure CLI (`az login`)

## Step 1: Create API App Registration

The API app registration exposes an API that the Blazor frontend will call.

### Using Azure Portal

1. Navigate to Azure Portal > Azure Active Directory > App registrations
2. Click "New registration"
3. Enter the following details:
   - **Name**: `TimeClock API - Dev`
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
   - **Redirect URI**: Leave empty for now
4. Click "Register"
5. Note down the **Application (client) ID** and **Directory (tenant) ID**

### Expose an API

1. In the API app registration, go to "Expose an API"
2. Click "Add a scope"
3. Accept the default Application ID URI (or set to `api://timeclock-api-dev`)
4. Add a scope:
   - **Scope name**: `access_as_user`
   - **Who can consent**: Admins and users
   - **Admin consent display name**: `Access TimeClock API as user`
   - **Admin consent description**: `Allow the application to access the TimeClock API on behalf of the signed-in user`
   - **User consent display name**: `Access TimeClock API`
   - **User consent description**: `Allow the application to access the TimeClock API on your behalf`
   - **State**: Enabled
5. Click "Add scope"

### Using Azure CLI

```bash
# Create API app registration
az ad app create \
  --display-name "TimeClock API - Dev" \
  --sign-in-audience AzureADMyOrg

# Get the Application ID (note this down)
API_APP_ID=$(az ad app list --display-name "TimeClock API - Dev" --query "[0].appId" -o tsv)
echo "API App ID: $API_APP_ID"

# Set the Application ID URI
az ad app update \
  --id $API_APP_ID \
  --identifier-uris "api://timeclock-api-dev"

# Expose the API scope (this creates the oauth2PermissionScopes)
# Note: The Azure CLI doesn't have a direct command for this, use the portal or the script below
```

## Step 2: Create Blazor App Registration

The Blazor app registration is used by the frontend to authenticate users.

### Using Azure Portal

1. Navigate to Azure Portal > Azure Active Directory > App registrations
2. Click "New registration"
3. Enter the following details:
   - **Name**: `TimeClock Blazor - Dev`
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
   - **Redirect URI**:
     - Type: Single-page application (SPA)
     - URI: `https://your-static-web-app-url.azurestaticapps.net/authentication/login-callback`
     - Add also: `http://localhost:5000/authentication/login-callback` for local development
4. Click "Register"
5. Note down the **Application (client) ID**

### Configure API Permissions

1. In the Blazor app registration, go to "API permissions"
2. Click "Add a permission"
3. Select "My APIs"
4. Select "TimeClock API - Dev"
5. Check the `access_as_user` scope
6. Click "Add permissions"
7. Click "Grant admin consent" (requires admin privileges)

### Using Azure CLI

```bash
# Create Blazor app registration
az ad app create \
  --display-name "TimeClock Blazor - Dev" \
  --sign-in-audience AzureADMyOrg \
  --web-redirect-uris "http://localhost:5000/authentication/login-callback" \
  --enable-access-token-issuance true \
  --enable-id-token-issuance true

# Get the Application ID (note this down)
BLAZOR_APP_ID=$(az ad app list --display-name "TimeClock Blazor - Dev" --query "[0].appId" -o tsv)
echo "Blazor App ID: $BLAZOR_APP_ID"

# Add the Static Web App redirect URI after deployment
az ad app update \
  --id $BLAZOR_APP_ID \
  --web-redirect-uris "http://localhost:5000/authentication/login-callback" "https://your-static-web-app-url.azurestaticapps.net/authentication/login-callback"

# Grant API permissions (requires the API scope ID)
# This is complex via CLI, recommend using the portal for this step
```

## Step 3: Update Application Configuration

After creating both app registrations, update your application configuration files.

### For the Backend API (TimeApi)

Update `appsettings.json` or use environment variables:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-tenant.onmicrosoft.com",
    "TenantId": "<YOUR_TENANT_ID>",
    "ClientId": "<API_APP_CLIENT_ID>",
    "Scopes": "access_as_user",
    "CallbackPath": "/signin-oidc"
  }
}
```

### For the Frontend Blazor WASM (TimeClockUI)

Update `wwwroot/appsettings.json`:

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<YOUR_TENANT_ID>",
    "ClientId": "<BLAZOR_APP_CLIENT_ID>",
    "ValidateAuthority": true
  },
  "ApiScopes": [
    "api://timeclock-api-dev/access_as_user"
  ],
  "ApiBaseUrl": "https://your-backend-api-url.azurecontainer.io"
}
```

## Step 4: Store Credentials in Key Vault (Optional)

If you want to store the Azure AD configuration in Key Vault:

```bash
# Store API ClientId
az keyvault secret set \
  --vault-name kv-timeclock-dev \
  --name "azuread-api-clientid" \
  --value "$API_APP_ID"

# Store Blazor ClientId
az keyvault secret set \
  --vault-name kv-timeclock-dev \
  --name "azuread-blazor-clientid" \
  --value "$BLAZOR_APP_ID"

# Store Tenant ID
TENANT_ID=$(az account show --query tenantId -o tsv)
az keyvault secret set \
  --vault-name kv-timeclock-dev \
  --name "azuread-tenantid" \
  --value "$TENANT_ID"
```

## Automated Setup Script

See `scripts/create-app-registrations.sh` for an automated setup script.

## Retrieving Configuration Values

### Get Tenant ID
```bash
az account show --query tenantId -o tsv
```

### Get Application IDs
```bash
# API App ID
az ad app list --display-name "TimeClock API - Dev" --query "[0].appId" -o tsv

# Blazor App ID
az ad app list --display-name "TimeClock Blazor - Dev" --query "[0].appId" -o tsv
```

### Get Application ID URI
```bash
az ad app show --id <API_APP_ID> --query identifierUris -o tsv
```

## Testing Authentication

1. Deploy both the API and Blazor app
2. Navigate to the Blazor app URL
3. Click the login button
4. You should be redirected to Microsoft login
5. After successful login, you should be redirected back to the app
6. The app should be able to call the API with the access token

## Troubleshooting

### CORS Issues
- Ensure the API has CORS configured to allow requests from the Blazor app domain
- Check that the redirect URIs match exactly (including trailing slashes)

### Invalid Audience
- Verify the `ClientId` in the API matches the API app registration
- Verify the scope in the Blazor app matches the exposed API scope

### Token Validation Failed
- Ensure the `TenantId` is correct in both applications
- Verify the API permissions are granted with admin consent
- Check that both apps are in the same Azure AD tenant

## Production Setup

For production, repeat the same steps but:
1. Use different app registration names (e.g., "TimeClock API - Prod")
2. Use production redirect URIs
3. Store production configuration in a separate Key Vault (`kv-timeclock-prod`)
4. Consider using different Azure AD tenants for dev/prod if required

## References

- [Microsoft Identity Platform Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [Secure ASP.NET Core Blazor WebAssembly](https://docs.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/)
- [Azure AD App Registration](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
