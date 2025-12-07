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

variable "backend_dns_label" {
  description = "DNS label for backend container (must be globally unique) - optional, will be auto-generated if not provided"
  type        = string
  default     = ""
}

variable "cors_allowed_origins" {
  description = "List of allowed CORS origins for the backend API"
  type        = list(string)
  default     = []
}

variable "cloudflare_tunnel_token" {
  description = "Cloudflare Tunnel token for HTTPS access (from GitHub Secrets)"
  type        = string
  sensitive   = true
}

# FinOps Tags
variable "cost_center" {
  description = "Cost center for billing and chargeback"
  type        = string
  default     = "engineering"
}

variable "owner" {
  description = "Owner of the resources (email or team name)"
  type        = string
  default     = ""
}

variable "budget_code" {
  description = "Budget code for tracking expenses"
  type        = string
  default     = ""
}

variable "application_name" {
  description = "Application name for resource grouping"
  type        = string
  default     = "timepunchclock"
}
