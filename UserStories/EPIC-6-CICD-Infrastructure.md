# Epic 6: CI/CD Pipeline - Infrastructure

## Goal
Implement GitHub Actions workflows for automated Terraform deployment and promotion from dev to prod with manual approval.

---

## US-601: Create Terraform Dev Deployment Workflow

**As a** developer
**I want** a GitHub Actions workflow to deploy dev infrastructure
**So that** infrastructure changes are automatically deployed to dev

### Acceptance Criteria
- [ ] Create `.github/workflows/terraform-dev.yml`
- [ ] Trigger on push to main branch when Infra/dev/** files change
- [ ] Configure Azure credentials using OIDC or Service Principal
- [ ] Run `terraform init`, `terraform plan`, `terraform apply`
- [ ] Store Terraform plan as artifact for review
- [ ] Configure backend state storage
- [ ] Add job to validate Terraform syntax first
- [ ] Only deploy if validation passes

### Technical Notes
```yaml
name: Terraform - Dev Deploy
on:
  push:
    branches: [main]
    paths:
      - 'Infra/dev/**'
  workflow_dispatch:
```

### Files to Create
- `.github/workflows/terraform-dev.yml`

---

## US-602: Create Terraform Prod Deployment Workflow with Approval

**As a** developer
**I want** a GitHub Actions workflow to deploy prod infrastructure with manual approval
**So that** production changes are reviewed before deployment

### Acceptance Criteria
- [ ] Create `.github/workflows/terraform-prod.yml`
- [ ] Trigger manually via workflow_dispatch
- [ ] Require manual approval before apply (use GitHub Environments)
- [ ] Create GitHub Environment named "production" with required reviewers
- [ ] Run `terraform init`, `terraform plan` automatically
- [ ] Wait for approval before `terraform apply`
- [ ] Notify reviewers when approval needed
- [ ] Show plan output in PR/workflow logs

### Technical Notes
- Use GitHub Environments feature for approvals
- Configure protection rules requiring 1+ reviewer

### Files to Create
- `.github/workflows/terraform-prod.yml`

---

## US-603: Configure GitHub Secrets for Terraform

**As a** developer
**I want** Azure credentials stored as GitHub secrets
**So that** workflows can authenticate to Azure securely

### Acceptance Criteria
- [ ] Create Service Principal for GitHub Actions
- [ ] Grant Contributor role to Service Principal on subscription/resource groups
- [ ] Add secrets to GitHub repository:
  - `AZURE_CLIENT_ID`
  - `AZURE_TENANT_ID`
  - `AZURE_SUBSCRIPTION_ID`
  - `AZURE_CLIENT_SECRET` (or configure OIDC)
- [ ] Document secret configuration process
- [ ] Test authentication in workflow
- [ ] Use separate Service Principals for dev/prod if needed

### Technical Notes
```bash
az ad sp create-for-rbac --name "sp-github-timeclock" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

### Files to Create
- `Infra/docs/github-actions-setup.md`

---

## US-604: Create Terraform Plan Preview for PRs

**As a** developer
**I want** Terraform plans posted as PR comments
**So that** I can review infrastructure changes before merging

### Acceptance Criteria
- [ ] Create workflow triggered on pull requests affecting Infra/**
- [ ] Run `terraform plan` for affected environments
- [ ] Post plan output as PR comment
- [ ] Show what resources will be added/changed/destroyed
- [ ] Update comment on subsequent commits
- [ ] Add validation status check to PR
- [ ] Block merge if Terraform validation fails

### Technical Notes
- Use `terraform-plan` action or custom script
- Format output for readability in GitHub comments

### Files to Create
- `.github/workflows/terraform-pr-preview.yml`

---

## Definition of Done (Epic 6)
- [ ] Dev infrastructure deploys automatically on merge to main
- [ ] Prod infrastructure requires manual approval
- [ ] Terraform plans visible in PR comments
- [ ] GitHub secrets configured for Azure authentication
- [ ] Workflows include validation and security checks
- [ ] Documentation complete for running workflows
- [ ] Test deployment to dev succeeds via workflow
- [ ] Code reviewed and merged to main branch
