#!/bin/bash

# Update Application Configuration from Terraform Outputs
# This script reads Terraform outputs and updates application config files

set -e

ENVIRONMENT=${1:-dev}

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}=== Updating Application Configuration for $ENVIRONMENT ===${NC}"

# Check if we're in the right directory
if [ ! -f "main.tf" ]; then
    echo -e "${RED}ERROR: Please run this script from the Infra/$ENVIRONMENT directory${NC}"
    exit 1
fi

# Check if terraform state exists
if [ ! -f ".terraform/terraform.tfstate" ] && [ ! -f "terraform.tfstate" ]; then
    echo -e "${RED}ERROR: No Terraform state found. Please run 'terraform apply' first.${NC}"
    exit 1
fi

# Initialize Terraform to ensure we can read outputs
echo "Initializing Terraform..."
terraform init -backend=false > /dev/null 2>&1 || true

# Get Terraform outputs
echo "Reading Terraform outputs..."

TENANT_ID=$(terraform output -raw azuread_tenant_id 2>/dev/null || echo "")
API_CLIENT_ID=$(terraform output -raw azuread_api_application_id 2>/dev/null || echo "")
BLAZOR_CLIENT_ID=$(terraform output -raw azuread_blazor_application_id 2>/dev/null || echo "")
API_SCOPE=$(terraform output -raw azuread_api_scope 2>/dev/null || echo "")
API_IDENTIFIER_URI=$(terraform output -raw azuread_api_identifier_uri 2>/dev/null || echo "")
BACKEND_URL=$(terraform output -raw backend_url 2>/dev/null || echo "")

if [ -z "$TENANT_ID" ] || [ -z "$API_CLIENT_ID" ] || [ -z "$BLAZOR_CLIENT_ID" ]; then
    echo -e "${RED}ERROR: Could not read Terraform outputs. Make sure Terraform apply has been run.${NC}"
    exit 1
fi

echo -e "${GREEN}Configuration Values:${NC}"
echo "  Tenant ID: $TENANT_ID"
echo "  API Client ID: $API_CLIENT_ID"
echo "  Blazor Client ID: $BLAZOR_CLIENT_ID"
echo "  API Scope: $API_SCOPE"
echo "  Backend URL: $BACKEND_URL"
echo ""

# Navigate to repo root (assuming we're in Infra/dev)
REPO_ROOT="../../"
cd "$REPO_ROOT"

# Update Backend API Configuration
API_CONFIG_FILE="src/TimeApi/appsettings.$ENVIRONMENT.json"

if [ "$ENVIRONMENT" = "dev" ]; then
    API_CONFIG_FILE="src/TimeApi/appsettings.Development.json"
fi

echo -e "${YELLOW}Updating Backend API Configuration...${NC}"

if [ -f "$API_CONFIG_FILE" ]; then
    # Backup original file
    cp "$API_CONFIG_FILE" "$API_CONFIG_FILE.backup"

    # Check if jq is available
    if command -v jq &> /dev/null; then
        # Use jq to update JSON
        jq --arg tenantId "$TENANT_ID" \
           --arg clientId "$API_CLIENT_ID" \
           --arg audience "$API_IDENTIFIER_URI" \
           '.AzureAd.TenantId = $tenantId |
            .AzureAd.ClientId = $clientId |
            .AzureAd.Audience = $audience' \
           "$API_CONFIG_FILE.backup" > "$API_CONFIG_FILE"

        echo -e "${GREEN}✓ Updated $API_CONFIG_FILE${NC}"
    else
        echo -e "${YELLOW}⚠ jq not found. Please install jq or manually update the file.${NC}"
        echo "  File: $API_CONFIG_FILE"
        echo "  Values needed:"
        echo "    TenantId: $TENANT_ID"
        echo "    ClientId: $API_CLIENT_ID"
        echo "    Audience: $API_IDENTIFIER_URI"
    fi
else
    echo -e "${YELLOW}⚠ File not found: $API_CONFIG_FILE${NC}"
    echo "Creating new configuration file..."

    mkdir -p "$(dirname "$API_CONFIG_FILE")"

    cat > "$API_CONFIG_FILE" <<EOF
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-tenant.onmicrosoft.com",
    "TenantId": "$TENANT_ID",
    "ClientId": "$API_CLIENT_ID",
    "Audience": "$API_IDENTIFIER_URI",
    "Scopes": "access_as_user",
    "CallbackPath": "/signin-oidc"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
EOF

    echo -e "${GREEN}✓ Created $API_CONFIG_FILE${NC}"
fi

# Update Frontend Blazor Configuration
BLAZOR_CONFIG_FILE="src/TimeClockUI/wwwroot/appsettings.$ENVIRONMENT.json"

if [ "$ENVIRONMENT" = "dev" ]; then
    BLAZOR_CONFIG_FILE="src/TimeClockUI/wwwroot/appsettings.Development.json"
fi

echo -e "${YELLOW}Updating Frontend Blazor Configuration...${NC}"

AUTHORITY="https://login.microsoftonline.com/$TENANT_ID"

if [ -f "$BLAZOR_CONFIG_FILE" ]; then
    # Backup original file
    cp "$BLAZOR_CONFIG_FILE" "$BLAZOR_CONFIG_FILE.backup"

    if command -v jq &> /dev/null; then
        # Use jq to update JSON
        jq --arg authority "$AUTHORITY" \
           --arg clientId "$BLAZOR_CLIENT_ID" \
           --arg apiScope "$API_SCOPE" \
           --arg apiBaseUrl "$BACKEND_URL" \
           '.AzureAd.Authority = $authority |
            .AzureAd.ClientId = $clientId |
            .ApiScopes = [$apiScope] |
            .ApiBaseUrl = $apiBaseUrl' \
           "$BLAZOR_CONFIG_FILE.backup" > "$BLAZOR_CONFIG_FILE"

        echo -e "${GREEN}✓ Updated $BLAZOR_CONFIG_FILE${NC}"
    else
        echo -e "${YELLOW}⚠ jq not found. Please install jq or manually update the file.${NC}"
        echo "  File: $BLAZOR_CONFIG_FILE"
        echo "  Values needed:"
        echo "    Authority: $AUTHORITY"
        echo "    ClientId: $BLAZOR_CLIENT_ID"
        echo "    ApiScopes: [$API_SCOPE]"
        echo "    ApiBaseUrl: $BACKEND_URL"
    fi
else
    echo -e "${YELLOW}⚠ File not found: $BLAZOR_CONFIG_FILE${NC}"
    echo "Creating new configuration file..."

    mkdir -p "$(dirname "$BLAZOR_CONFIG_FILE")"

    cat > "$BLAZOR_CONFIG_FILE" <<EOF
{
  "AzureAd": {
    "Authority": "$AUTHORITY",
    "ClientId": "$BLAZOR_CLIENT_ID",
    "ValidateAuthority": true
  },
  "ApiScopes": [
    "$API_SCOPE"
  ],
  "ApiBaseUrl": "$BACKEND_URL"
}
EOF

    echo -e "${GREEN}✓ Created $BLAZOR_CONFIG_FILE${NC}"
fi

echo ""
echo -e "${GREEN}=== Configuration Update Complete ===${NC}"
echo ""
echo "Next steps:"
echo "1. Review the updated configuration files"
echo "2. Build and test your applications locally"
echo "3. Deploy to Azure"
echo ""
echo "Backup files created (in case you need to revert):"
echo "  - $API_CONFIG_FILE.backup"
echo "  - $BLAZOR_CONFIG_FILE.backup"
