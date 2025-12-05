# Terraform Cost Estimation & Approval Setup

This guide explains how to configure the automated cost estimation and approval workflow for Terraform deployments.

## Overview

The `terraform-dev.yml` workflow now includes:
- **Automated cost estimation** using Infracost
- **Manual approval requirement** before deploying infrastructure
- **Two-stage deployment**: Plan → Review → Approve → Deploy

## Required Setup

### 1. Get Infracost API Key

Infracost provides free cost estimates for Terraform.

1. Sign up at [https://www.infracost.io/](https://www.infracost.io/)
2. Generate an API key from your dashboard
3. The free tier includes unlimited cost estimates

### 2. Add GitHub Secret

Add the Infracost API key to your GitHub repository:

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Name: `INFRACOST_API_KEY`
5. Value: Your Infracost API key
6. Click **Add secret**

### 3. Configure Environment Protection

Enable manual approval for the `dev` environment:

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Environments**
3. Click on **dev** (or create it if it doesn't exist)
4. Under **Deployment protection rules**, check **Required reviewers**
5. Add yourself (and any other approvers) to the reviewers list
6. Click **Save protection rules**

## How It Works

### Workflow Stages

1. **Plan Stage** (`terraform-plan` job):
   - Runs on every push to `main` that changes infrastructure files
   - Performs Terraform format check, init, validate, and plan
   - Generates a cost estimate using Infracost
   - Displays cost breakdown in the GitHub Actions summary
   - Uploads the Terraform plan as an artifact

2. **Deploy Stage** (`terraform-deploy` job):
   - **Requires manual approval** (configured in Environment settings)
   - Downloads the plan from the previous stage
   - Applies the approved Terraform plan
   - Outputs infrastructure details

### Cost Estimate Output

The workflow will show:
- Monthly cost estimate for new resources
- Cost difference from current infrastructure
- Breakdown by resource type
- Total monthly cost

### Approval Process

1. Push changes to `main` or trigger workflow manually
2. Wait for the **Plan** job to complete
3. Review the cost estimate in the Actions summary
4. If costs look good, approve the deployment:
   - Go to the Actions tab
   - Click on the running workflow
   - Click **Review deployments**
   - Select **dev** environment
   - Click **Approve and deploy**
5. The **Deploy** job will run automatically after approval

## Optional: Skip Infracost

If you don't want to use Infracost, you can:

1. Remove the Infracost steps from the workflow
2. Keep the manual approval (environment protection)
3. The workflow will still require approval but won't show cost estimates

## Cost Estimate Accuracy

Infracost provides estimates based on Azure's public pricing:
- Estimates are based on default usage patterns
- Actual costs may vary based on:
  - Actual resource usage
  - Reserved instances or discounts
  - Data transfer and storage usage
- Use estimates as a guideline, not exact predictions

## Troubleshooting

### "INFRACOST_API_KEY secret not found"
- Ensure you've added the secret in GitHub Settings → Secrets and variables → Actions

### "Environment protection rules"
- Go to Settings → Environments → dev
- Ensure "Required reviewers" is enabled
- Add at least one reviewer

### Plan artifact not found
- The plan is saved for 5 days
- If the deploy job runs after 5 days, you'll need to re-run the plan

## Example Cost Output

```
💰 Infracost estimate: monthly cost will increase by $45 ↑

Name                                    Monthly Qty  Unit            Monthly Cost

azurerm_mssql_database.main
└─ Compute (S1)                         730          hours           $30.00
└─ Storage                              10           GB              $1.15

azurerm_container_group.backend
└─ Container instance (1 vCPU, 1.5 GB)  730          hours           $13.14

Total                                                                 $45.00
```
