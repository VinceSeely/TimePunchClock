# ⚠️⚠️⚠️ TEMPORARY FILE - DELETE WHEN DONE ⚠️⚠️⚠️
#
# This file serves as a visual reminder that temporary Azure AD admin access
# is currently configured for the SQL Server.
#
# WHAT THIS DOES:
# - Grants Azure AD admin rights to: Sophia Seely (2634c4b8-e02b-4771-a58a-36ab905ad887)
# - Allows authentication from DataGrip or other SQL tools using Azure AD
# - Provides FULL administrative access to the SQL Server
#
# CREATED: 2025-12-08
# PURPOSE: Manual database management during development
#
# HOW TO REMOVE THIS ACCESS:
# 1. Delete this file (temp-sql-admin.tf)
# 2. Delete temp-sql-admin.tfvars
# 3. Delete TEMP-ACCESS-README.md
# 4. Run: terraform plan (should show azuread_administrator being removed)
# 5. Run: terraform apply
# 6. Verify: terraform state show azurerm_mssql_server.main | grep azuread
#
# SECURITY NOTES:
# - This grants FULL server admin privileges
# - All database operations are audited and logged
# - Azure AD MFA policies are enforced
# - Access should be removed when no longer needed
#
# ⚠️⚠️⚠️ REMEMBER TO CLEAN UP ⚠️⚠️⚠️

# This file intentionally contains no Terraform resources.
# The actual configuration is in main.tf using the dynamic block:
#
# dynamic "azuread_administrator" {
#   for_each = var.azuread_admin_object_id != "" ? [1] : []
#   content {
#     login_username = var.azuread_admin_login
#     object_id      = var.azuread_admin_object_id
#   }
# }
#
# Variables are set in temp-sql-admin.tfvars
