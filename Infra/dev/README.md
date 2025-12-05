# TimeClock Dev Infrastructure

This directory contains Terraform configuration for the TimeClock development environment.

## What Gets Created

This Terraform configuration automatically provisions:

### Core Infrastructure
- **Resource Group**: Container for all resources
- **Azure Key Vault**: Secure storage for secrets and configuration
- **SQL Server & Database**: Azure SQL with Basic tier
- **Container Registry**: Azure Container Registry for Docker images
- **Container Instance**: Runs the backend API
- **Static Web App**: Hosts the Blazor WASM frontend

### Azure AD App Registrations (New!)
- **API App Registration**: Backend API authentication
- **Blazor App Registration**: Frontend SPA authentication
- **Automatic Scope Configuration**: API exposes `access_as_user` scope
- **Automatic Permission Grants**: Admin consent for Blazor to call API

### Security Features
- Random password generation for SQL Server
- All secrets stored in Key Vault
- Connection strings secured via Key Vault references
- Azure AD configuration stored in Key Vault

## Prerequisites

1. **Azure CLI** installed and authenticated
   ```bash
   az login
   ```

2. **Terraform** installed (v1.6.0 or later)
   ```bash
   terraform version
   ```

3. **Required Azure Permissions**:
   - Contributor role on the subscription or resource group
   - Application Administrator or Global Administrator role in Azure AD (for app registrations)
   - Permissions to grant admin consent for API permissions

## Quick Start

### 1. Initialize Terraform

```bash
cd Infra/dev
terraform init
```

### 2. Review and Customize (Optional)

The configuration uses sensible defaults from `locals.tf`. You only need to modify `terraform.tfvars` if you want to override:
- SQL server name (otherwise auto-generated)
- Container registry name (otherwise auto-generated)
- Your IP address for SQL firewall access

### 3. Plan the Deployment

```bash
terraform plan
```

Review what will be created. This should show:
- 1 Resource Group
- 1 Key Vault with secrets
- 1 SQL Server and Database
- 1 Container Registry
- 1 Container Instance
- 1 Static Web App
- 2 Azure AD Applications (API and Blazor)
- 2 Service Principals

### 4. Deploy

```bash
terraform apply
```

Type `yes` when prompted.

### 5. View Configuration

After deployment, view the Azure AD configuration:

```bash
terraform output azuread_configuration_summary
```

Or get specific values:

```bash
# Azure AD Tenant ID
terraform output azuread_tenant_id

# API Application ID
terraform output azuread_api_application_id

# Blazor Application ID
terraform output azuread_blazor_application_id

# API Scope
terraform output azuread_api_scope
```

## Configuration Values

All Azure AD configuration is automatically stored in Key Vault:
- `azuread-tenant-id`
- `azuread-api-client-id`
- `azuread-blazor-client-id`
- `azuread-api-scope`
- `azuread-api-identifier-uri`

Retrieve from Key Vault:
```bash
az keyvault secret show --vault-name kv-timeclock-dev --name azuread-api-client-id --query value -o tsv
```

## Updating Application Configuration

### Backend API (TimeApi)

Update `src/TimeApi/appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<from terraform output: azuread_tenant_id>",
    "ClientId": "<from terraform output: azuread_api_application_id>",
    "Audience": "<from terraform output: azuread_api_identifier_uri>"
  }
}
```

### Frontend Blazor (TimeClockUI)

Update `src/TimeClockUI/wwwroot/appsettings.Development.json`:

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<TENANT_ID>",
    "ClientId": "<from terraform output: azuread_blazor_application_id>",
    "ValidateAuthority": true
  },
  "ApiScopes": [
    "<from terraform output: azuread_api_scope>"
  ],
  "ApiBaseUrl": "<from terraform output: backend_url>"
}
```

Or use the helper script:
```bash
# Coming soon: Script to automatically update config files
./scripts/update-app-config.sh dev
```

## Admin Consent

The Terraform configuration attempts to grant admin consent automatically via the `azuread_service_principal_delegated_permission_grant` resource.

If this fails due to insufficient permissions, manually grant consent:

1. Go to Azure Portal > Azure Active Directory > App registrations
2. Select "TimeClock Blazor - dev"
3. Go to "API permissions"
4. Click "Grant admin consent for [Your Organization]"

Or use Azure CLI:
```bash
BLAZOR_APP_ID=$(terraform output -raw azuread_blazor_application_id)
az ad app permission admin-consent --id $BLAZOR_APP_ID
```

## Troubleshooting

### "Insufficient privileges to complete the operation"

This error occurs when your account doesn't have permission to create app registrations.

**Solution**: Ask an Azure AD administrator to:
1. Grant you "Application Administrator" role, OR
2. Run the Terraform deployment for you, OR
3. Enable "Users can register applications" in Azure AD settings

### App Registration Already Exists

If you get an error that the app registration already exists:

```bash
# Import existing app into Terraform state
terraform import azuread_application.api <existing-app-object-id>
terraform import azuread_application.blazor <existing-app-object-id>
```

### Permission Grants Not Working

If admin consent fails, you can manually grant it or remove the `azuread_service_principal_delegated_permission_grant` resource from `azuread.tf` and grant consent manually.

## Clean Up

To destroy all resources:

```bash
terraform destroy
```

**Warning**: This will delete:
- All Azure resources
- Azure AD app registrations
- All data in the SQL database

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Azure AD Tenant                       │
│  ┌────────────────────┐         ┌──────────────────────┐   │
│  │ API App            │         │ Blazor App           │   │
│  │ Registration       │◄────────│ Registration         │   │
│  │                    │ permits │                      │   │
│  │ Scope:             │         │ Permissions:         │   │
│  │ access_as_user     │         │ - API access_as_user │   │
│  └────────────────────┘         └──────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                    │                        │
                    │ authenticates          │ authenticates
                    ▼                        ▼
         ┌─────────────────┐      ┌────────────────────┐
         │ Container       │      │ Static Web App     │
         │ Instance        │◄─────│ (Blazor WASM)      │
         │ (Backend API)   │ calls│                    │
         └────────┬────────┘      └────────────────────┘
                  │
                  │ reads secrets
                  ▼
         ┌─────────────────┐      ┌────────────────────┐
         │ Key Vault       │      │ SQL Database       │
         │                 │      │                    │
         │ - SQL password  │      │ - TimeClock data   │
         │ - Connection    │◄─────┤                    │
         │   strings       │      └────────────────────┘
         │ - Azure AD IDs  │
         └─────────────────┘
```

## What's Different from Manual Setup

Compared to the manual Azure AD setup documentation:

✅ **Now Automated**:
- App registration creation
- Scope definition
- Redirect URI configuration
- Permission requests
- Service principal creation
- Admin consent (if you have permissions)
- Key Vault storage

❌ **Still Manual** (if needed):
- Admin consent (if Terraform lacks permissions)
- Application secrets/certificates (not needed for this app)
- Advanced authentication scenarios

## Next Steps

1. Update application configuration files with Terraform outputs
2. Deploy backend API to Container Registry
3. Deploy frontend to Static Web App
4. Test authentication flow

See the main repository README for deployment instructions.

## Files in This Directory

- `main.tf` - Core infrastructure resources
- `azuread.tf` - Azure AD app registrations (NEW!)
- `locals.tf` - Local values and naming conventions
- `variables.tf` - Input variables
- `outputs.tf` - Output values
- `terraform.tfvars` - Variable values (customize as needed)
- `README.md` - This file

## Additional Resources

- [Azure AD App Registration Docs](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Terraform AzureAD Provider](https://registry.terraform.io/providers/hashicorp/azuread/latest/docs)
- [Original Manual Setup Guide](./azure-ad-setup.md) - For reference
