# Epic 4: Infrastructure as Code - Dev Environment

## Goal
Update Terraform configuration for dev environment with proper secrets management, minimal variable dependencies, and Azure AD integration.

---

## US-401: Configure Azure Key Vault for Secrets Management

**As a** developer
**I want** Terraform to create and manage Azure Key Vault for storing secrets
**So that** sensitive values are not stored in code or state files

### Acceptance Criteria
- [ ] Add Azure Key Vault resource to `main.tf`
- [ ] Configure access policies for deployed applications
- [ ] Enable soft delete and purge protection
- [ ] Set appropriate SKU (Standard)
- [ ] Add network rules if needed
- [ ] Grant current user access during development

### Technical Notes
```hcl
resource "azurerm_key_vault" "kv" {
  name                = "kv-timeclock-${var.environment}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  tenant_id          = data.azurerm_client_config.current.tenant_id
  sku_name           = "standard"
}
```

### Files to Modify
- `Infra/dev/main.tf`

---

## US-402: Generate Random Passwords with Terraform

**As a** developer
**I want** Terraform to generate random passwords for SQL Server
**So that** credentials are secure and not hardcoded

### Acceptance Criteria
- [ ] Use `random_password` resource for SQL admin password
- [ ] Set appropriate length (16+ characters) and complexity
- [ ] Mark password as sensitive in outputs
- [ ] Store generated password in Key Vault
- [ ] Remove hardcoded passwords from `terraform.tfvars`
- [ ] Document password retrieval process for developers

### Technical Notes
```hcl
resource "random_password" "sql_admin_password" {
  length  = 24
  special = true
  upper   = true
  lower   = true
  numeric = true
}

resource "azurerm_key_vault_secret" "sql_password" {
  name         = "sql-admin-password"
  value        = random_password.sql_admin_password.result
  key_vault_id = azurerm_key_vault.kv.id
}
```

### Files to Modify
- `Infra/dev/main.tf`
- `Infra/dev/variables.tf` (remove password variables)
- `Infra/dev/terraform.tfvars` (remove password values)

---

## US-403: Add Azure AD App Registrations Configuration

**As a** developer
**I want** documentation for creating Azure AD app registrations
**So that** authentication can be properly configured

### Acceptance Criteria
- [ ] Create `Infra/dev/azure-ad-setup.md` documentation
- [ ] Document app registration for API (backend)
- [ ] Document app registration for Blazor app (frontend)
- [ ] Document required API permissions and scopes
- [ ] Document redirect URIs for Blazor WASM
- [ ] Include Azure CLI scripts for automated setup
- [ ] Document how to get TenantId and ClientIds

### Technical Notes
- API app registration needs exposed API scope (e.g., `api://timeclock-api/access_as_user`)
- Blazor app registration needs API permissions to call API
- Consider using Terraform azuread provider in future

### Files to Create
- `Infra/dev/azure-ad-setup.md`
- `Infra/dev/scripts/create-app-registrations.sh`

---

## US-404: Update Connection Strings to Use Key Vault References

**As a** developer
**I want** application connection strings to reference Key Vault secrets
**So that** sensitive data is not exposed in Terraform state or logs

### Acceptance Criteria
- [ ] Store SQL connection string in Key Vault
- [ ] Configure Container Instance to use Key Vault references for secrets
- [ ] Configure API app to read connection string from environment variables
- [ ] Use managed identity for Key Vault access where possible
- [ ] Remove connection string from terraform.tfvars
- [ ] Test connection string retrieval in deployed container

### Technical Notes
- Container Instance can use secure environment variables
- Connection string format: `Server=<server>.database.windows.net;Database=<db>;User Id=<user>;Password=<password>;`

### Files to Modify
- `Infra/dev/main.tf` (Key Vault secrets, Container Instance config)
- `Infra/dev/outputs.tf` (add Key Vault outputs)

---

## US-405: Minimize Variable Dependencies and Self-Contain Dev Config

**As a** developer
**I want** the dev Terraform configuration to be self-contained
**So that** it can be deployed with minimal external dependencies

### Acceptance Criteria
- [ ] Review all variables in `variables.tf`
- [ ] Remove unnecessary variables or provide sensible defaults
- [ ] Use locals for computed values instead of variables where appropriate
- [ ] Ensure `terraform.tfvars` only contains environment-specific values
- [ ] Add `locals.tf` for shared configuration within module
- [ ] Document required vs optional variables
- [ ] Test deployment with minimal `terraform.tfvars`

### Technical Notes
- Variables should only be used for values that differ between dev/prod
- Use naming conventions with environment suffix instead of variables

### Files to Modify
- `Infra/dev/variables.tf`
- `Infra/dev/terraform.tfvars`

### Files to Create
- `Infra/dev/locals.tf`

---

## Definition of Done (Epic 4)
- [ ] `terraform plan` runs successfully in dev folder
- [ ] No hardcoded passwords or secrets in code
- [ ] All secrets stored in Azure Key Vault
- [ ] SQL admin password randomly generated
- [ ] Connection strings use Key Vault references
- [ ] Documentation complete for Azure AD setup
- [ ] Minimal variables required in terraform.tfvars
- [ ] Dev environment can be deployed from scratch
- [ ] Code reviewed and merged to main branch
