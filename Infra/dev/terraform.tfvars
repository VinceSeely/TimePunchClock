# Terraform variables for dev environment
# Most values are auto-generated from locals.tf
# Only specify these if you need to override defaults

# Optional: Specify custom SQL server name (otherwise auto-generated)
# sql_server_name = "sql-timeclock-custom-name"

# Optional: Specify custom ACR name (otherwise auto-generated)
# acr_name = "acrtimeclockcustom"

# Optional: Specify custom backend DNS label (otherwise auto-generated)
# backend_dns_label = "timeclock-api-custom"

# SQL admin username (default: sqladmin)
# sql_admin_username = "sqladmin"

# Azure AD admin configuration (optional)
azuread_admin_login     = ""
azuread_admin_object_id = ""

# Your IP address for SQL firewall access (optional, set to 0.0.0.0 to disable)
my_ip_address = "0.0.0.0"
