# GitHub Actions Setup for TimeClock

This document describes how to configure GitHub Actions workflows for automated infrastructure and application deployment.

## Overview

The TimeClock project uses GitHub Actions for CI/CD with the following workflows:
- **terraform-pr-preview.yml** - Validates and previews Terraform changes in PRs
- **terraform-dev.yml** - Automatically deploys infrastructure to dev on merge to main
- **terraform-prod.yml** - Manually deploys infrastructure to prod with approval
- **backend-build.yml** - Builds and tests backend API
- **backend-deploy-dev.yml** - Deploys backend to dev automatically
- **backend-deploy-prod.yml** - Deploys backend to prod with approval
- **frontend-deploy-dev.yml** - Deploys frontend to dev automatically
- **frontend-deploy-prod.yml** - Deploys frontend to prod with approval
- **deploy-complete.yml** - Full stack deployment orchestration

## Prerequisites

- Azure subscription with appropriate permissions
- GitHub repository for the TimeClock project
- Azure CLI installed locally for initial setup

## Step 1: Create Azure Service Principal

Create a service principal for GitHub Actions to authenticate with Azure.

### Option A: Using Azure CLI with OIDC (Recommended)

OIDC (OpenID Connect) allows GitHub Actions to authenticate without storing credentials:

```bash
# Set variables
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RESOURCE_GROUP="rg-timeclock-dev"
APP_NAME="sp-github-timeclock"

# Create the service principal
az ad sp create-for-rbac \
  --name "$APP_NAME" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID \
  --sdk-auth

# Note down the output, you'll need:
# - clientId
# - tenantId
# - subscriptionId
```

### Option B: Using Azure CLI with Client Secret (Legacy)

```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az ad sp create-for-rbac \
  --name "sp-github-timeclock" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID \
  --sdk-auth

# This will output JSON with:
# - clientId
# - clientSecret (store this securely!)
# - tenantId
# - subscriptionId
```

### Grant Additional Permissions

The service principal needs permissions to:
1. Create and manage Azure resources
2. Assign roles (for Key Vault access policies)
3. Read from Key Vault
4. **Create and manage Azure AD app registrations** (NEW!)

```bash
# Get the service principal object ID
SP_OBJECT_ID=$(az ad sp list --display-name "sp-github-timeclock" --query "[0].id" -o tsv)

# Grant User Access Administrator role (for managing Key Vault access)
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --role "User Access Administrator" \
  --scope /subscriptions/$SUBSCRIPTION_ID

# Grant Azure AD permissions for managing app registrations
# The service principal needs "Application Administrator" role in Azure AD
az rest --method POST --url "https://graph.microsoft.com/v1.0/roleManagement/directory/roleAssignments" \
  --headers "Content-Type=application/json" \
  --body "{
    \"principalId\": \"$SP_OBJECT_ID\",
    \"roleDefinitionId\": \"9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3\",
    \"directoryScopeId\": \"/\"
  }"

# Note: The roleDefinitionId above is for "Application Administrator" role
# This is required for Terraform to create Azure AD app registrations

# If using specific resource groups instead of subscription-wide access:
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --role "Contributor" \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-timeclock-dev

az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --role "Contributor" \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-timeclock-prod
```

**Important**: If you cannot grant Application Administrator role, you can:
1. Create the Azure AD app registrations manually first (see `Infra/dev/azure-ad-setup.md`)
2. Comment out the `azuread.tf` file in your Terraform configuration
3. Or have an Azure AD administrator run the Terraform deployment

## Step 2: Configure GitHub Secrets

Add the following secrets to your GitHub repository:

1. Go to your GitHub repository
2. Navigate to Settings > Secrets and variables > Actions
3. Click "New repository secret" for each secret below

### Required Secrets

| Secret Name | Description | How to Get |
|------------|-------------|------------|
| `AZURE_CLIENT_ID` | Service Principal Application (client) ID | From Step 1 output: `clientId` |
| `AZURE_TENANT_ID` | Azure AD Tenant ID | From Step 1 output: `tenantId` |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID | From Step 1 output: `subscriptionId` or `az account show --query id -o tsv` |
| `AZURE_CLIENT_SECRET` | Service Principal Secret (if not using OIDC) | From Step 1 output: `clientSecret` |

### Azure CLI Commands to Retrieve Values

```bash
# Get Tenant ID
az account show --query tenantId -o tsv

# Get Subscription ID
az account show --query id -o tsv

# Get Service Principal details
az ad sp list --display-name "sp-github-timeclock" --query "[0].appId" -o tsv
```

## Step 3: Configure GitHub Environments

GitHub Environments provide deployment protection rules and environment-specific secrets.

### Create Environments

1. Go to your GitHub repository
2. Navigate to Settings > Environments
3. Create two environments: `dev` and `production`

### Configure Production Environment Protection

For the `production` environment:
1. Enable "Required reviewers"
2. Add yourself (and team members) as reviewers
3. Enable "Wait timer" (optional, e.g., 5 minutes)
4. Optionally limit deployment to specific branches (e.g., `main`)

### Environment-Specific Secrets (Optional)

You can override secrets per environment if dev/prod use different service principals:

In each environment, add:
- `AZURE_CLIENT_ID` (environment-specific)
- `AZURE_CLIENT_SECRET` (environment-specific)
- Any other environment-specific values

## Step 4: Configure Terraform Backend State

Set up Azure Storage for Terraform remote state.

### Create Storage Account

```bash
# Variables
RESOURCE_GROUP="rg-timeclock-terraform"
LOCATION="eastus"
STORAGE_ACCOUNT="sttimeclockstate"
CONTAINER_NAME="tfstate"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --encryption-services blob

# Get storage account key
ACCOUNT_KEY=$(az storage account keys list \
  --resource-group $RESOURCE_GROUP \
  --account-name $STORAGE_ACCOUNT \
  --query '[0].value' -o tsv)

# Create blob container
az storage container create \
  --name $CONTAINER_NAME \
  --account-name $STORAGE_ACCOUNT \
  --account-key $ACCOUNT_KEY
```

### Update Terraform Configuration

Add this backend configuration to `Infra/dev/main.tf` and `Infra/prod/main.tf`:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-timeclock-terraform"
    storage_account_name = "sttimeclockstate"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"  # Use "prod.terraform.tfstate" for prod
  }

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    # ... rest of providers
  }
}
```

### Add Storage Access to GitHub Secrets

```bash
# Add storage account name and key as secrets
echo "AZURE_STORAGE_ACCOUNT: $STORAGE_ACCOUNT"
echo "AZURE_STORAGE_KEY: $ACCOUNT_KEY"
```

Add these to GitHub secrets:
- `AZURE_STORAGE_ACCOUNT`
- `AZURE_STORAGE_KEY`

Or grant the service principal access to the storage account:

```bash
SP_OBJECT_ID=$(az ad sp list --display-name "sp-github-timeclock" --query "[0].id" -o tsv)

az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --role "Storage Blob Data Contributor" \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT
```

## Step 5: Test Workflows

### Test Terraform PR Preview

1. Create a new branch
2. Make a change to any file in `Infra/`
3. Create a pull request
4. Verify that the Terraform plan appears as a comment

### Test Dev Deployment

1. Merge a PR that changes `Infra/dev/**`
2. The `terraform-dev.yml` workflow should trigger automatically
3. Check the Actions tab for progress

### Test Prod Deployment

1. Go to Actions tab
2. Select "Terraform - Prod Deploy"
3. Click "Run workflow"
4. Approve the deployment when prompted

## Step 6: Configure Additional Secrets for Application Deployment

For application deployment workflows, add these additional secrets:

| Secret Name | Description |
|------------|-------------|
| `ACR_USERNAME` | Container Registry admin username (from Terraform output) |
| `ACR_PASSWORD` | Container Registry admin password (from Terraform output) |
| `SWA_DEPLOYMENT_TOKEN_DEV` | Static Web App deployment token for dev |
| `SWA_DEPLOYMENT_TOKEN_PROD` | Static Web App deployment token for prod |

### Get ACR Credentials

```bash
# After Terraform creates the ACR
az acr credential show --name <acr-name> --resource-group <rg-name>
```

### Get Static Web App Token

```bash
# Get deployment token for dev
az staticwebapp secrets list \
  --name <swa-name> \
  --resource-group <rg-name> \
  --query "properties.apiKey" -o tsv

# Or from Terraform output
cd Infra/dev
terraform output -raw static_web_app_api_key
```

## Troubleshooting

### Authentication Failures

If workflows fail with authentication errors:

1. Verify secrets are correctly set in GitHub
2. Check service principal has correct permissions:
   ```bash
   az role assignment list --assignee <client-id> --all
   ```
3. Ensure service principal is not expired

### Terraform State Lock Issues

If Terraform state is locked:

```bash
# List locks
az storage blob lease list \
  --container-name tfstate \
  --account-name $STORAGE_ACCOUNT

# Break lock if needed (use with caution!)
az storage blob lease break \
  --blob-name dev.terraform.tfstate \
  --container-name tfstate \
  --account-name $STORAGE_ACCOUNT
```

### Insufficient Permissions

If you get permission denied errors:

```bash
# Check current permissions
az role assignment list --assignee <sp-object-id> --all

# Add missing role
az role assignment create \
  --assignee-object-id <sp-object-id> \
  --role "Contributor" \
  --scope /subscriptions/$SUBSCRIPTION_ID
```

## Security Best Practices

1. **Use OIDC instead of client secrets** when possible
2. **Limit service principal scope** to specific resource groups
3. **Enable branch protection** on main branch
4. **Require reviews** for production deployments
5. **Rotate secrets regularly** (every 90 days)
6. **Use environment secrets** for sensitive environment-specific values
7. **Enable audit logs** in Azure AD for service principal activity

## Workflow Triggers

### Automatic Triggers

| Workflow | Trigger |
|----------|---------|
| terraform-pr-preview | Pull request affecting `Infra/**` |
| terraform-dev | Push to `main` affecting `Infra/dev/**` |
| backend-build | Push to `main` or PR affecting `src/TimeApi/**` |
| backend-deploy-dev | After backend build succeeds on `main` |
| frontend-deploy-dev | Push to `main` affecting `src/TimeClockUI/**` |

### Manual Triggers

| Workflow | How to Trigger |
|----------|---------------|
| terraform-prod | Actions tab > Select workflow > Run workflow |
| backend-deploy-prod | Actions tab > Select workflow > Run workflow |
| frontend-deploy-prod | Actions tab > Select workflow > Run workflow |
| deploy-complete | Actions tab > Select workflow > Run workflow |

## Next Steps

1. Set up monitoring and alerting for workflow failures
2. Configure Slack/Teams notifications for deployments
3. Set up automated testing in workflows
4. Configure branch protection rules
5. Document rollback procedures

## References

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Login Action](https://github.com/Azure/login)
- [Terraform GitHub Actions](https://github.com/hashicorp/setup-terraform)
- [Azure Service Principal Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal)
