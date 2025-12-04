# Copy this file to terraform.tfvars and fill in your values

resource_group_name = "rg-blazor-app"
location            = "eastus"
environment         = "dev"

# SQL Server settings (server name must be globally unique)
sql_server_name     = "sql-blazor-unique-12345"
sql_database_name   = "blazordb"
sql_admin_username  = "sqladmin"
sql_admin_password  = "YourSecurePassword123!"

# Azure AD admin (optional, can be empty strings)
azuread_admin_login     = ""
azuread_admin_object_id = ""

# Your IP address for SQL firewall access
my_ip_address = "0.0.0.0"  # Replace with your actual IP

# Container Registry (name must be globally unique and alphanumeric only)
acr_name = "acrblazorunique12345"

# Backend container settings
backend_container_name = "backend-api"
backend_image_name     = "backend-api"
backend_image_tag      = "latest"

# Static Web App
static_web_app_name = "blazor-spa"