# Variables for dev environment
# Only environment-specific or user-provided values should be variables

variable "sql_server_name" {
  description = "Name of the SQL Server (must be globally unique) - optional, will be auto-generated if not provided"
  type        = string
  default     = ""
}

variable "sql_admin_username" {
  description = "SQL Server administrator username"
  type        = string
  default     = "sqladmin"
  sensitive   = true
}

variable "azuread_admin_login" {
  description = "Azure AD admin login name (optional)"
  type        = string
  default     = ""
}

variable "azuread_admin_object_id" {
  description = "Azure AD admin object ID (optional)"
  type        = string
  default     = ""
}

variable "my_ip_address" {
  description = "Your IP address for SQL firewall rule (optional, use 0.0.0.0 to skip)"
  type        = string
  default     = "0.0.0.0"
}

variable "acr_name" {
  description = "Name of the Azure Container Registry (must be globally unique, alphanumeric only) - optional, will be auto-generated if not provided"
  type        = string
  default     = ""
}

variable "backend_dns_label" {
  description = "DNS label for backend container (must be globally unique) - optional, will be auto-generated if not provided"
  type        = string
  default     = ""
}
