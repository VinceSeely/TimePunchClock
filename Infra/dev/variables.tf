variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "rg-blazor-app"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "eastus"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "dev"
}

variable "sql_server_name" {
  description = "Name of the SQL Server (must be globally unique)"
  type        = string
}

variable "sql_database_name" {
  description = "Name of the SQL Database"
  type        = string
  default     = "blazordb"
}

variable "sql_admin_username" {
  description = "SQL Server administrator username"
  type        = string
  sensitive   = true
}

variable "sql_admin_password" {
  description = "SQL Server administrator password"
  type        = string
  sensitive   = true
}

variable "azuread_admin_login" {
  description = "Azure AD admin login name"
  type        = string
  default     = ""
}

variable "azuread_admin_object_id" {
  description = "Azure AD admin object ID"
  type        = string
  default     = ""
}

variable "my_ip_address" {
  description = "Your IP address for SQL firewall rule"
  type        = string
}

variable "acr_name" {
  description = "Name of the Azure Container Registry (must be globally unique, alphanumeric only)"
  type        = string
}

variable "backend_container_name" {
  description = "Name of the backend container group"
  type        = string
  default     = "backend-api"
}

variable "backend_dns_label" {
  description = "DNS label for backend container (must be globally unique)"
  type        = string
}

variable "backend_image_name" {
  description = "Name of the backend Docker image"
  type        = string
  default     = "backend-api"
}

variable "backend_image_tag" {
  description = "Tag of the backend Docker image"
  type        = string
  default     = "latest"
}

variable "static_web_app_name" {
  description = "Name of the Static Web App for Blazor SPA"
  type        = string
  default     = "blazor-spa"
}