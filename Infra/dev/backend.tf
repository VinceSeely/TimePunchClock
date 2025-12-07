# Terraform backend configuration for remote state
# State is stored in Azure Storage Account

terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "tfstate387180"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"
    use_oidc             = true
    # tenant_id and client_id are set via ARM_TENANT_ID and ARM_CLIENT_ID environment variables
  }
}
