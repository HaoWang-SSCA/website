# SSCA-BC Website Infrastructure

## Overview

Terraform configuration for Azure infrastructure:
- **Azure Static Web App** - Hosts Blazor WASM frontend + Azure Functions API
- **Azure PostgreSQL Flexible Server** - Database for meeting data
- **Azure Storage Account** - Blob storage for audio files

## Prerequisites

1. [Terraform](https://www.terraform.io/downloads) >= 1.0
2. [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
3. Azure subscription with required permissions

## Quick Start

```bash
cd infrastructure

# Login to Azure
az login
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# Step 1: Create state storage (run once)
chmod +x bootstrap-state.sh
./bootstrap-state.sh

# Step 2: Uncomment backend block in main.tf

# Step 3: Initialize Terraform
terraform init

# Step 4: Create terraform.tfvars from example
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values

# Step 5: Plan and apply
terraform plan -var="postgres_admin_password=YOUR_SECURE_PASSWORD"
terraform apply -var="postgres_admin_password=YOUR_SECURE_PASSWORD"
```

## Post-Deployment Steps

### 1. Get Static Web App API Token
```bash
terraform output -raw static_web_app_api_key
```
Add this as `AZURE_STATIC_WEB_APPS_API_TOKEN` in GitHub repository secrets.

### 2. Get Connection Strings
```bash
# PostgreSQL connection string
terraform output -raw postgres_connection_string

# Storage connection string
terraform output -raw storage_account_connection_string
```
Add these to Azure Static Web App → Configuration → Application settings:
- `ConnectionStrings__PostgreSQL`
- `ConnectionStrings__AzureStorage`
- `AzureStorage__BaseUrl` = `<storage_blob_endpoint>` output value

### 3. Run Database Migrations
```bash
cd ../src/SSCA.website.API
dotnet ef migrations add Initial
dotnet ef database update --connection "YOUR_CONNECTION_STRING"
```

## Cost Estimation (Monthly)

| Resource | SKU | Est. Cost |
|----------|-----|-----------|
| Static Web App | Free | $0 |
| PostgreSQL Flexible | B_Standard_B1ms | ~$15 |
| Storage Account | Standard LRS | ~$1-5 |
| **Total** | | **~$20/month** |

## Files

| File | Description |
|------|-------------|
| `main.tf` | Provider configuration |
| `variables.tf` | Input variable definitions |
| `resources.tf` | Resource definitions |
| `outputs.tf` | Output values |
| `terraform.tfvars.example` | Example configuration |

## Cleanup

```bash
terraform destroy -var="postgres_admin_password=YOUR_PASSWORD"
```
