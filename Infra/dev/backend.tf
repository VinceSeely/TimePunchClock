# Terraform backend configuration for remote state
# State is stored in Azure Storage Account

terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "tfstate387180"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"
  }
}
