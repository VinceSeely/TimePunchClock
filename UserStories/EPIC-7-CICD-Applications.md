# Epic 7: CI/CD Pipeline - Applications

## Goal
Implement GitHub Actions workflows for building, testing, and deploying the frontend (Blazor WASM) and backend (API) applications to dev and prod environments.

---

## US-701: Create Backend API Build Workflow

**As a** developer
**I want** a workflow to build and test the backend API
**So that** code quality is maintained and builds are automated

### Acceptance Criteria
- [ ] Create `.github/workflows/backend-build.yml`
- [ ] Trigger on push to main and pull requests affecting src/TimeApi/**
- [ ] Restore NuGet packages
- [ ] Build TimeApi project
- [ ] Run unit tests (if any exist)
- [ ] Build Docker image
- [ ] Tag image with commit SHA and "latest"
- [ ] Run security scanning on dependencies
- [ ] Cache NuGet packages for faster builds

### Technical Notes
```yaml
name: Backend - Build & Test
on:
  push:
    branches: [main]
    paths:
      - 'src/TimeApi/**'
      - 'src/TimeClock.client/**'
  pull_request:
    paths:
      - 'src/TimeApi/**'
      - 'src/TimeClock.client/**'
```

### Files to Create
- `.github/workflows/backend-build.yml`

---

## US-702: Create Backend API Deploy to Dev Workflow

**As a** developer
**I want** the backend API automatically deployed to dev
**So that** changes are immediately available for testing

### Acceptance Criteria
- [ ] Create `.github/workflows/backend-deploy-dev.yml`
- [ ] Trigger after successful backend build on main branch
- [ ] Push Docker image to Azure Container Registry (dev)
- [ ] Restart Azure Container Instance with new image
- [ ] Run database migrations automatically
- [ ] Verify deployment health check
- [ ] Post deployment status to Slack/Teams (optional)

### Technical Notes
- Use `az acr build` or docker build + docker push
- May need to stop/start container instance to pull new image

### Files to Create
- `.github/workflows/backend-deploy-dev.yml`

---

## US-703: Create Backend API Deploy to Prod Workflow

**As a** developer
**I want** backend API deployed to prod with manual approval
**So that** production deployments are controlled

### Acceptance Criteria
- [ ] Create `.github/workflows/backend-deploy-prod.yml`
- [ ] Trigger manually via workflow_dispatch
- [ ] Require approval using GitHub Environments ("production")
- [ ] Push Docker image to Azure Container Registry (prod)
- [ ] Run database migrations with approval step
- [ ] Create backup before deployment
- [ ] Restart Azure Container Instance
- [ ] Run smoke tests after deployment
- [ ] Rollback capability if deployment fails

### Technical Notes
- Consider blue-green deployment or staging slot
- Ensure migrations are backwards compatible

### Files to Create
- `.github/workflows/backend-deploy-prod.yml`

---

## US-704: Create Frontend Blazor Build & Deploy to Dev Workflow

**As a** developer
**I want** the Blazor frontend automatically built and deployed to dev
**So that** UI changes are immediately available

### Acceptance Criteria
- [ ] Create `.github/workflows/frontend-deploy-dev.yml`
- [ ] Trigger on push to main affecting src/TimeClockUI/**
- [ ] Build Blazor WASM project in Release mode
- [ ] Inject Azure AD configuration from secrets
- [ ] Deploy to Azure Static Web Apps (dev)
- [ ] Verify deployment with health check
- [ ] Post deployment URL in workflow summary

### Technical Notes
- Use Azure Static Web Apps GitHub Action
- Configuration values need to be injected at build time

```bash
dotnet publish -c Release -o publish
```

### Files to Create
- `.github/workflows/frontend-deploy-dev.yml`

---

## US-705: Create Frontend Blazor Deploy to Prod Workflow

**As a** developer
**I want** the Blazor frontend deployed to prod with manual approval
**So that** production releases are controlled

### Acceptance Criteria
- [ ] Create `.github/workflows/frontend-deploy-prod.yml`
- [ ] Trigger manually via workflow_dispatch
- [ ] Require approval using GitHub Environments
- [ ] Build Blazor WASM with production configuration
- [ ] Inject production Azure AD settings
- [ ] Deploy to Azure Static Web Apps (prod)
- [ ] Run end-to-end tests after deployment (if exist)
- [ ] Create GitHub release tag after successful deployment

### Technical Notes
- Production build should use different Azure AD ClientIds
- Consider cache busting for static assets

### Files to Create
- `.github/workflows/frontend-deploy-prod.yml`

---

## US-706: Create End-to-End Deployment Workflow

**As a** developer
**I want** a single workflow to deploy infrastructure and applications together
**So that** complete environment setup is automated

### Acceptance Criteria
- [ ] Create `.github/workflows/deploy-complete.yml`
- [ ] Deploy infrastructure first (Terraform)
- [ ] Wait for infrastructure to be ready
- [ ] Deploy backend API
- [ ] Run database migrations
- [ ] Deploy frontend
- [ ] Run smoke tests on full stack
- [ ] Support both dev and prod environments via input parameter
- [ ] Require approval for prod deployments

### Technical Notes
- This workflow orchestrates others or duplicates steps
- Useful for disaster recovery and new environment setup

### Files to Create
- `.github/workflows/deploy-complete.yml`

---

## Definition of Done (Epic 7)
- [ ] Backend builds automatically on push to main
- [ ] Backend deploys to dev automatically after successful build
- [ ] Backend deploys to prod with manual approval
- [ ] Frontend deploys to dev automatically on push
- [ ] Frontend deploys to prod with manual approval
- [ ] Docker images tagged with commit SHA
- [ ] Database migrations run automatically in dev
- [ ] Smoke tests validate deployments
- [ ] Rollback procedure documented
- [ ] Code reviewed and merged to main branch
