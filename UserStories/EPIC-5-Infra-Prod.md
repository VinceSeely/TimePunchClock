# Epic 5: Infrastructure as Code - Prod Environment

## Goal
Create production Terraform configuration with separate state management and resource groups from dev.

---

## US-501: Create Prod Terraform Directory Structure

**As a** developer
**I want** a separate Terraform configuration for production
**So that** prod and dev environments are isolated

### Acceptance Criteria
- [ ] Create `Infra/prod/` directory
- [ ] Copy and adapt dev configuration files to prod
- [ ] Update resource naming conventions (add "prod" suffix/prefix)
- [ ] Create separate `terraform.tfvars` for prod values
- [ ] Create separate backend configuration for state storage
- [ ] Document differences between dev and prod configurations

### Technical Notes
- Consider using Terraform modules for shared configuration
- Prod should use different resource group name
- State file should be stored in separate Azure Storage container

### Files to Create
- `Infra/prod/main.tf`
- `Infra/prod/variables.tf`
- `Infra/prod/terraform.tfvars`
- `Infra/prod/outputs.tf`
- `Infra/prod/locals.tf`
- `Infra/prod/backend.tf`
- `Infra/prod/README.md`

---

## US-502: Configure Separate Backend State for Prod

**As a** developer
**I want** production Terraform state stored separately from dev
**So that** changes to one environment don't affect the other

### Acceptance Criteria
- [ ] Create Azure Storage Account for Terraform state (if not exists)
- [ ] Create separate blob container for prod state (`tfstate-prod`)
- [ ] Configure backend in `Infra/prod/backend.tf`
- [ ] Enable state locking with Azure Storage
- [ ] Enable versioning on storage container
- [ ] Document state storage configuration
- [ ] Test state initialization with `terraform init`

### Technical Notes
```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "sttfstate<unique>"
    container_name       = "tfstate-prod"
    key                  = "timeclock.tfstate"
  }
}
```

### Files to Create
- `Infra/prod/backend.tf`
- `Infra/scripts/create-backend-storage.sh`

---

## US-503: Configure Production-Specific Settings

**As a** developer
**I want** production configuration to use production-grade settings
**So that** the prod environment is optimized for reliability and performance

### Acceptance Criteria
- [ ] SQL Database tier: Standard S1 or higher (not Basic)
- [ ] Container Instance: Higher CPU/memory allocation
- [ ] Azure Container Registry: Standard or Premium tier
- [ ] Enable Azure AD authentication on SQL Server
- [ ] Configure stricter firewall rules
- [ ] Enable diagnostic logging and monitoring
- [ ] Configure backup retention policies
- [ ] Add resource locks to prevent accidental deletion

### Technical Notes
- Prod should have tighter security than dev
- Consider enabling Azure Defender for SQL
- May need different pricing tiers

### Files to Modify
- `Infra/prod/main.tf`
- `Infra/prod/locals.tf`

---

## Definition of Done (Epic 5)
- [ ] Prod Terraform configuration exists separately from dev
- [ ] Prod uses separate resource group
- [ ] Prod uses separate state storage
- [ ] Prod configuration uses production-grade settings
- [ ] Documentation explains dev vs prod differences
- [ ] `terraform plan` runs successfully in prod folder
- [ ] Resource locks configured to prevent accidental deletion
- [ ] Code reviewed and merged to main branch
