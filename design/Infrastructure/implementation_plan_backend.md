# Fix Terraform Backend Configuration

## Goal Description
Fix the Terraform initialization error: `Error: One of 'access_key', 'sas_token', 'use_azuread_auth' and 'resource_group_name' must be specified`.

## Proposed Changes
### Infrastructure
#### [MODIFY] [main.tf](file:///c:/Projects/HaoWang-SSCA/website/infrastructure/main.tf)
- Update `backend "azurerm"` block to include `resource_group_name = "terraform-state-rg"` to resolve initialization error.

## Verification Plan
### Automated Tests
- Run `terraform init` to verify the error is resolved.
