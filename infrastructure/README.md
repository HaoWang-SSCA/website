# SSCA-BC Website Infrastructure

## Overview

Multi-environment Terraform configuration for Azure infrastructure:
- **Azure Static Web App** - Hosts Blazor WASM frontend
- **Azure Function App** (Independent) - Hosts .NET 8 isolated worker API
- **Azure Storage Account** - Blob storage for audio files and bulletins
- **Application Insights** - Monitoring and diagnostics
- **PostgreSQL** (Shared Server) - Database for meeting data

## Architecture

```
infrastructure/
├── modules/
│   └── ssca-website/          # Shared Terraform module
│       ├── main.tf            # Provider requirements
│       ├── variables.tf       # Input variables
│       ├── resources.tf       # All Azure resources
│       └── outputs.tf         # Output values
├── environments/
│   ├── test/
│   │   └── main.tf            # Test env config (state: ssca-website-test.tfstate)
│   └── prod/
│       └── main.tf            # Prod env config (state: ssca-website-prod.tfstate)
├── bootstrap-state.sh         # One-time state storage setup
└── README.md                  # This file
```

## Environments

| Environment | Branch | SWA SKU | Function Plan | Database | State Key |
|-------------|--------|---------|---------------|----------|-----------|
| **Test** | `main` | Free | Y1 (Consumption) | `ssca` | `ssca-website-test.tfstate` |
| **Production** | `prod` | Standard | Y1 (Consumption) | `ssca-prod` | `ssca-website-prod.tfstate` |

## Prerequisites

1. [Terraform](https://www.terraform.io/downloads) >= 1.0
2. [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
3. Azure subscription with required permissions
4. State storage bootstrapped (run `bootstrap-state.sh` once)

## Quick Start

```bash
# Login to Azure
az login

# Set ARM_ACCESS_KEY for state storage
export ARM_ACCESS_KEY="$(az storage account keys list --account-name hwtfstate -o tsv --query '[0].value')"

# Deploy Test environment
cd infrastructure/environments/test
terraform init
terraform plan -var="postgres_username=YOUR_USER" -var="postgres_password=YOUR_PASS"
terraform apply -var="postgres_username=YOUR_USER" -var="postgres_password=YOUR_PASS"

# Deploy Production environment
cd infrastructure/environments/prod
terraform init
terraform plan -var="postgres_username=YOUR_USER" -var="postgres_password=YOUR_PASS"
terraform apply -var="postgres_username=YOUR_USER" -var="postgres_password=YOUR_PASS"
```

## Post-Deployment Steps

### 1. Get Deployment Tokens
```bash
# From the environment directory (test/ or prod/)
terraform output -raw static_web_app_api_key
terraform output -raw function_app_name
```

### 2. Configure GitHub Environments

Create two GitHub Environments: `test` and `production`.

For each environment, set these secrets:

| Secret | Description |
|--------|-------------|
| `SWA_DEPLOY_TOKEN` | Static Web App API key (from terraform output) |
| `FUNCTION_APP_NAME` | Function App name (from terraform output) |
| `AZURE_CREDENTIALS` | Azure Service Principal JSON |
| `ARM_ACCESS_KEY` | Terraform state storage access key |
| `POSTGRES_USERNAME` | PostgreSQL username |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `AAD_CLIENT_ID` | Azure AD app client ID |
| `AAD_CLIENT_SECRET` | Azure AD app client secret |
| `GH_PAT` | GitHub PAT for updating secrets |

### 3. Set up `prod` branch protection

```bash
# Create prod branch from main
git checkout -b prod
git push origin prod
```

In GitHub repo Settings → Branches → Add rule for `prod`:
- ✅ Require pull request reviews before merging
- ✅ Require status checks to pass

In GitHub repo Settings → Environments → `production`:
- ✅ Required reviewers (add yourself)

## CI/CD Workflows

| Workflow | Trigger | Environment |
|----------|---------|-------------|
| `deploy-test.yml` | Push to `main` (src/**) / Manual | test |
| `deploy-prod.yml` | Push to `prod` (src/**) / Manual | production |
| `terraform-test.yml` | Push to `main` (infrastructure/**) / Manual | test |
| `terraform-prod.yml` | Push to `prod` (infrastructure/**) / Manual | production |

All workflows support `workflow_dispatch` for manual triggering.

## Cost Estimation (Monthly, per environment)

| Resource | SKU | Test | Production |
|----------|-----|------|------------|
| Static Web App | Free/Standard | $0 | ~$9 |
| Function App | Consumption (Y1) | ~$0 | ~$0 |
| Storage Account | Standard LRS | ~$1-5 | ~$1-5 |
| App Insights | Pay-as-you-go | ~$0 | ~$0 |
| PostgreSQL | Shared server | shared | shared |
| **Total** | | **~$1-5** | **~$10-15** |

## Cleanup

```bash
cd infrastructure/environments/test  # or prod
terraform destroy -var="postgres_username=USER" -var="postgres_password=PASS"
```
