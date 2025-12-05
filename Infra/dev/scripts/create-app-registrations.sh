#!/bin/bash

# Azure AD App Registrations Setup Script for TimeClock Application
# This script creates the necessary Azure AD app registrations for dev environment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== TimeClock Azure AD Setup - Dev Environment ===${NC}"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}ERROR: Azure CLI is not installed. Please install it first.${NC}"
    exit 1
fi

# Check if logged in
echo "Checking Azure CLI login status..."
if ! az account show &> /dev/null; then
    echo -e "${YELLOW}Not logged in. Please log in to Azure...${NC}"
    az login
fi

# Get current tenant and subscription
TENANT_ID=$(az account show --query tenantId -o tsv)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
ACCOUNT_NAME=$(az account show --query user.name -o tsv)

echo -e "${GREEN}Logged in as:${NC} $ACCOUNT_NAME"
echo -e "${GREEN}Tenant ID:${NC} $TENANT_ID"
echo -e "${GREEN}Subscription:${NC} $SUBSCRIPTION_ID"
echo ""

# Configuration
ENVIRONMENT="dev"
API_APP_NAME="TimeClock API - $ENVIRONMENT"
BLAZOR_APP_NAME="TimeClock Blazor - $ENVIRONMENT"
API_APP_URI="api://timeclock-api-$ENVIRONMENT"

# Prompt for redirect URIs
echo -e "${YELLOW}Enter the Static Web App URL for redirect URI (or press Enter to skip):${NC}"
read -r STATIC_WEB_APP_URL

if [ -z "$STATIC_WEB_APP_URL" ]; then
    STATIC_WEB_APP_URL="https://your-static-web-app.azurestaticapps.net"
    echo -e "${YELLOW}Using placeholder URL: $STATIC_WEB_APP_URL${NC}"
fi

REDIRECT_URI_SPA="$STATIC_WEB_APP_URL/authentication/login-callback"
REDIRECT_URI_LOCAL="http://localhost:5000/authentication/login-callback"

echo ""
echo -e "${GREEN}=== Step 1: Creating API App Registration ===${NC}"

# Check if API app already exists
EXISTING_API_APP=$(az ad app list --display-name "$API_APP_NAME" --query "[0].appId" -o tsv 2>/dev/null)

if [ -n "$EXISTING_API_APP" ]; then
    echo -e "${YELLOW}API app registration already exists with ID: $EXISTING_API_APP${NC}"
    echo "Do you want to delete and recreate it? (y/n)"
    read -r RECREATE_API
    if [ "$RECREATE_API" = "y" ]; then
        echo "Deleting existing API app registration..."
        az ad app delete --id "$EXISTING_API_APP"
        sleep 5
    else
        API_APP_ID=$EXISTING_API_APP
    fi
fi

if [ -z "$API_APP_ID" ]; then
    echo "Creating API app registration..."
    az ad app create \
        --display-name "$API_APP_NAME" \
        --sign-in-audience AzureADMyOrg \
        --output none

    API_APP_ID=$(az ad app list --display-name "$API_APP_NAME" --query "[0].appId" -o tsv)
    echo -e "${GREEN}Created API app registration${NC}"
    sleep 2
fi

echo -e "${GREEN}API App ID:${NC} $API_APP_ID"

# Set Application ID URI
echo "Setting Application ID URI..."
az ad app update --id "$API_APP_ID" --identifier-uris "$API_APP_URI" --output none

# Create the oauth2 permission scope
echo "Exposing API scope..."
SCOPE_ID=$(uuidgen 2>/dev/null || cat /proc/sys/kernel/random/uuid 2>/dev/null || echo "00000000-0000-0000-0000-000000000001")

cat > /tmp/api-manifest.json <<EOF
{
    "oauth2PermissionScopes": [
        {
            "adminConsentDescription": "Allow the application to access the TimeClock API on behalf of the signed-in user",
            "adminConsentDisplayName": "Access TimeClock API as user",
            "id": "$SCOPE_ID",
            "isEnabled": true,
            "type": "User",
            "userConsentDescription": "Allow the application to access the TimeClock API on your behalf",
            "userConsentDisplayName": "Access TimeClock API",
            "value": "access_as_user"
        }
    ]
}
EOF

az ad app update --id "$API_APP_ID" --set api=@/tmp/api-manifest.json --output none
rm /tmp/api-manifest.json

echo -e "${GREEN}API scope 'access_as_user' created${NC}"
echo ""

echo -e "${GREEN}=== Step 2: Creating Blazor App Registration ===${NC}"

# Check if Blazor app already exists
EXISTING_BLAZOR_APP=$(az ad app list --display-name "$BLAZOR_APP_NAME" --query "[0].appId" -o tsv 2>/dev/null)

if [ -n "$EXISTING_BLAZOR_APP" ]; then
    echo -e "${YELLOW}Blazor app registration already exists with ID: $EXISTING_BLAZOR_APP${NC}"
    echo "Do you want to delete and recreate it? (y/n)"
    read -r RECREATE_BLAZOR
    if [ "$RECREATE_BLAZOR" = "y" ]; then
        echo "Deleting existing Blazor app registration..."
        az ad app delete --id "$EXISTING_BLAZOR_APP"
        sleep 5
    else
        BLAZOR_APP_ID=$EXISTING_BLAZOR_APP
    fi
fi

if [ -z "$BLAZOR_APP_ID" ]; then
    echo "Creating Blazor app registration..."
    az ad app create \
        --display-name "$BLAZOR_APP_NAME" \
        --sign-in-audience AzureADMyOrg \
        --enable-access-token-issuance true \
        --enable-id-token-issuance true \
        --output none

    BLAZOR_APP_ID=$(az ad app list --display-name "$BLAZOR_APP_NAME" --query "[0].appId" -o tsv)
    echo -e "${GREEN}Created Blazor app registration${NC}"
    sleep 2
fi

echo -e "${GREEN}Blazor App ID:${NC} $BLAZOR_APP_ID"

# Configure SPA redirect URIs
echo "Configuring redirect URIs..."
cat > /tmp/blazor-manifest.json <<EOF
{
    "spa": {
        "redirectUris": [
            "$REDIRECT_URI_LOCAL",
            "$REDIRECT_URI_SPA"
        ]
    }
}
EOF

az ad app update --id "$BLAZOR_APP_ID" --set @/tmp/blazor-manifest.json --output none
rm /tmp/blazor-manifest.json

echo -e "${GREEN}Redirect URIs configured${NC}"
echo ""

echo -e "${GREEN}=== Step 3: Configuring API Permissions ===${NC}"

# Add API permission
echo "Adding API permission to Blazor app..."

# Get the API's OAuth2 permission ID
API_PERMISSION_ID=$(az ad app show --id "$API_APP_ID" --query "api.oauth2PermissionScopes[0].id" -o tsv)

cat > /tmp/permissions.json <<EOF
{
    "requiredResourceAccess": [
        {
            "resourceAppId": "$API_APP_ID",
            "resourceAccess": [
                {
                    "id": "$API_PERMISSION_ID",
                    "type": "Scope"
                }
            ]
        }
    ]
}
EOF

az ad app update --id "$BLAZOR_APP_ID" --set @/tmp/permissions.json --output none
rm /tmp/permissions.json

echo -e "${GREEN}API permissions configured${NC}"
echo ""
echo -e "${YELLOW}NOTE: Admin consent is required. Run the following command if you have admin privileges:${NC}"
echo "az ad app permission admin-consent --id $BLAZOR_APP_ID"
echo ""

echo -e "${GREEN}=== Step 4: Summary ===${NC}"
echo ""
echo -e "${GREEN}Tenant ID:${NC} $TENANT_ID"
echo -e "${GREEN}API App Registration:${NC}"
echo "  Name: $API_APP_NAME"
echo "  Client ID: $API_APP_ID"
echo "  App ID URI: $API_APP_URI"
echo "  Scope: $API_APP_URI/access_as_user"
echo ""
echo -e "${GREEN}Blazor App Registration:${NC}"
echo "  Name: $BLAZOR_APP_NAME"
echo "  Client ID: $BLAZOR_APP_ID"
echo "  Redirect URIs:"
echo "    - $REDIRECT_URI_LOCAL"
echo "    - $REDIRECT_URI_SPA"
echo ""

# Store in Key Vault if it exists
echo -e "${YELLOW}Do you want to store these values in Azure Key Vault? (y/n)${NC}"
read -r STORE_IN_KV

if [ "$STORE_IN_KV" = "y" ]; then
    echo "Enter Key Vault name (e.g., kv-timeclock-dev):"
    read -r KV_NAME

    if [ -n "$KV_NAME" ]; then
        echo "Storing secrets in Key Vault $KV_NAME..."

        az keyvault secret set --vault-name "$KV_NAME" --name "azuread-tenantid" --value "$TENANT_ID" --output none
        az keyvault secret set --vault-name "$KV_NAME" --name "azuread-api-clientid" --value "$API_APP_ID" --output none
        az keyvault secret set --vault-name "$KV_NAME" --name "azuread-blazor-clientid" --value "$BLAZOR_APP_ID" --output none
        az keyvault secret set --vault-name "$KV_NAME" --name "azuread-api-scope" --value "$API_APP_URI/access_as_user" --output none

        echo -e "${GREEN}Secrets stored in Key Vault successfully${NC}"
    fi
fi

echo ""
echo -e "${GREEN}=== Setup Complete! ===${NC}"
echo ""
echo "Next steps:"
echo "1. Grant admin consent for API permissions (if you have admin rights)"
echo "2. Update your application configuration files with the above values"
echo "3. Deploy your applications"
echo "4. Test authentication flow"
echo ""
echo "For more details, see azure-ad-setup.md"
