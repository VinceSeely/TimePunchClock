# Azure AD App Registrations - Now Automated with Terraform! 🎉

## What Changed

Azure AD app registrations are now **fully automated** using Terraform! No more manual clicking through the Azure Portal or running complex CLI scripts.

## What Gets Created Automatically

When you run `terraform apply` in `Infra/dev/`, Terraform will now create:

### API App Registration (Backend)
- ✅ Application registration named "TimeClock API - dev"
- ✅ Service principal for the application
- ✅ Identifier URI: `api://timeclock-api-dev`
- ✅ OAuth2 permission scope: `access_as_user`
- ✅ Proper audience configuration

### Blazor App Registration (Frontend)
- ✅ Application registration named "TimeClock Blazor - dev"
- ✅ Service principal for the application
- ✅ SPA redirect URIs (localhost + Static Web App URL)
- ✅ API permissions to call the backend API
- ✅ Microsoft Graph permissions for user profile
- ✅ Admin consent granted automatically

### Configuration Storage
- ✅ All Azure AD IDs stored in Key Vault
- ✅ Configuration values available as Terraform outputs
- ✅ Easy retrieval for application configuration

## File Structure

```
Infra/dev/
├── main.tf              # Core infrastructure
├── azuread.tf           # ⭐ NEW! Azure AD app registrations
├── locals.tf            # Local values
├── variables.tf         # Input variables
├── outputs.tf           # Updated with Azure AD outputs
├── terraform.tfvars     # Variable values
├── scripts/
│   ├── create-app-registrations.sh  # Legacy manual script (backup)
│   └── update-app-config.sh         # ⭐ NEW! Auto-update app configs
├── azure-ad-setup.md    # Manual setup docs (for reference)
└── README.md            # ⭐ UPDATED! Complete setup guide
```

## How to Use

### 1. Prerequisites

Your Azure service principal (for Terraform/GitHub Actions) needs:
- **Contributor** role on the subscription/resource group
- **Application Administrator** role in Azure AD (for app registrations)

Grant the Azure AD role:
```bash
SP_OBJECT_ID=$(az ad sp list --display-name "sp-github-timeclock" --query "[0].id" -o tsv)

az rest --method POST --url "https://graph.microsoft.com/v1.0/roleManagement/directory/roleAssignments" \
  --headers "Content-Type=application/json" \
  --body "{
    \"principalId\": \"$SP_OBJECT_ID\",
    \"roleDefinitionId\": \"9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3\",
    \"directoryScopeId\": \"/\"
  }"
```

### 2. Deploy with Terraform

```bash
cd Infra/dev
terraform init
terraform plan   # Review what will be created
terraform apply  # Create everything including Azure AD apps
```

### 3. View Configuration

After deployment:
```bash
# See full Azure AD configuration summary
terraform output azuread_configuration_summary

# Get specific values
terraform output azuread_tenant_id
terraform output azuread_api_application_id
terraform output azuread_blazor_application_id
terraform output azuread_api_scope
```

### 4. Update Application Configuration

**Option A: Automatic (Recommended)**
```bash
cd Infra/dev
./scripts/update-app-config.sh dev
```

This script automatically updates:
- `src/TimeApi/appsettings.Development.json`
- `src/TimeClockUI/wwwroot/appsettings.Development.json`

**Option B: Manual**

Get values and update manually:
```bash
terraform output azuread_configuration_summary
```

## What Terraform Creates

### In Azure AD

```hcl
# azuread.tf creates:

resource "azuread_application" "api" {
  display_name = "TimeClock API - dev"
  identifier_uris = ["api://timeclock-api-dev"]

  api {
    oauth2_permission_scope {
      value = "access_as_user"
      # ... full configuration
    }
  }
}

resource "azuread_application" "blazor" {
  display_name = "TimeClock Blazor - dev"

  single_page_application {
    redirect_uris = [
      "http://localhost:5000/authentication/login-callback",
      "https://your-static-web-app.azurestaticapps.net/authentication/login-callback"
    ]
  }

  required_resource_access {
    resource_app_id = azuread_application.api.client_id
    # Requests access_as_user permission
  }
}

# Admin consent granted automatically
resource "azuread_service_principal_delegated_permission_grant" "blazor_to_api" {
  # ...
}
```

### In Key Vault

All configuration is automatically stored:
- `azuread-tenant-id`
- `azuread-api-client-id`
- `azuread-blazor-client-id`
- `azuread-api-scope`
- `azuread-api-identifier-uri`

## Benefits

### ✅ Before (Manual)
- ❌ 30+ manual steps in Azure Portal
- ❌ Error-prone copy/paste of IDs
- ❌ Hard to replicate across environments
- ❌ No version control
- ❌ Manual documentation needed

### ✅ After (Terraform)
- ✅ One command: `terraform apply`
- ✅ Automated and consistent
- ✅ Easy to replicate (dev, staging, prod)
- ✅ Version controlled in Git
- ✅ Self-documenting via Terraform code

## Troubleshooting

### "Insufficient privileges to complete the operation"

**Problem**: Your account/service principal lacks Azure AD permissions.

**Solution**:
1. Grant "Application Administrator" role (see prerequisites)
2. Or create app registrations manually first, then import:
   ```bash
   terraform import azuread_application.api <api-app-object-id>
   terraform import azuread_application.blazor <blazor-app-object-id>
   ```
3. Or comment out `azuread.tf` and use manual setup

### Admin Consent Fails

**Problem**: Terraform can't grant admin consent automatically.

**Solution**: Grant it manually:
```bash
BLAZOR_APP_ID=$(terraform output -raw azuread_blazor_application_id)
az ad app permission admin-consent --id $BLAZOR_APP_ID
```

Or in Azure Portal:
1. Go to Azure AD > App registrations
2. Select "TimeClock Blazor - dev"
3. Go to API permissions
4. Click "Grant admin consent for [Your Org]"

### App Already Exists

**Problem**: App registration with the same name already exists.

**Solution**: Import existing app:
```bash
# Get the existing app's object ID from Azure Portal or CLI
terraform import azuread_application.api <object-id>
terraform import azuread_application.blazor <object-id>
```

## Migration from Manual Setup

If you already created Azure AD apps manually:

### Option 1: Import Existing (Recommended)
```bash
# Get object IDs
API_OBJECT_ID=$(az ad app list --display-name "TimeClock API - dev" --query "[0].id" -o tsv)
BLAZOR_OBJECT_ID=$(az ad app list --display-name "TimeClock Blazor - dev" --query "[0].id" -o tsv)

# Import into Terraform
cd Infra/dev
terraform import azuread_application.api $API_OBJECT_ID
terraform import azuread_application.blazor $BLAZOR_OBJECT_ID

# Terraform will now manage your existing apps
terraform plan
```

### Option 2: Delete and Recreate
```bash
# Delete existing apps
az ad app delete --id <api-app-id>
az ad app delete --id <blazor-app-id>

# Let Terraform create new ones
cd Infra/dev
terraform apply
```

## Environment Strategy

### Dev Environment
```bash
cd Infra/dev
terraform apply
# Creates: "TimeClock API - dev" and "TimeClock Blazor - dev"
```

### Prod Environment (Future)
```bash
cd Infra/prod
terraform apply
# Will create: "TimeClock API - prod" and "TimeClock Blazor - prod"
```

Each environment gets its own:
- App registrations
- Client IDs
- Redirect URIs
- API scopes

## Checking Your Setup

After running `terraform apply`:

```bash
# 1. Check Terraform outputs
terraform output azuread_configuration_summary

# 2. Verify in Azure Portal
# Go to Azure AD > App registrations
# You should see:
#   - TimeClock API - dev
#   - TimeClock Blazor - dev

# 3. Check Key Vault
az keyvault secret list --vault-name kv-timeclock-dev --query "[?starts_with(name, 'azuread')].name"

# 4. Test authentication
# Deploy your apps and try logging in!
```

## Architecture Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    Terraform Apply                          │
│                                                              │
│  1. Creates Azure Infrastructure (SQL, ACR, etc.)          │
│  2. Creates Azure AD App Registrations ⭐ NEW!             │
│  3. Configures OAuth2 Scopes                               │
│  4. Sets up Redirect URIs                                  │
│  5. Grants Admin Consent                                   │
│  6. Stores All Config in Key Vault                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  Terraform Outputs                          │
│                                                              │
│  - Tenant ID                                               │
│  - API Application ID                                      │
│  - Blazor Application ID                                   │
│  - API Scope                                               │
│  - Full Configuration Summary                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│            Update Application Config (script)                │
│                                                              │
│  ./scripts/update-app-config.sh dev                        │
│                                                              │
│  Updates:                                                   │
│  - src/TimeApi/appsettings.Development.json                │
│  - src/TimeClockUI/wwwroot/appsettings.Development.json   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    🚀 Deploy & Test!
```

## Additional Resources

- **Terraform AzureAD Provider**: https://registry.terraform.io/providers/hashicorp/azuread/latest/docs
- **Azure AD App Registration**: https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app
- **Original Manual Setup Guide**: `Infra/dev/azure-ad-setup.md` (kept for reference)
- **Terraform Configuration**: `Infra/dev/azuread.tf`

## Summary

🎉 **You no longer need to manually create Azure AD app registrations!**

Just run `terraform apply` and everything is created automatically:
- API app registration ✅
- Blazor app registration ✅
- OAuth2 scopes ✅
- Redirect URIs ✅
- Admin consent ✅
- Key Vault storage ✅

**One command. Fully automated. Repeatable across environments.**
