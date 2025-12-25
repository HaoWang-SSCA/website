# Terraform Backend Configuration Fix

## Changes
### Infrastructure
#### [MODIFY] [main.tf](file:///c:/Projects/HaoWang-SSCA/website/infrastructure/main.tf)
- Added `resource_group_name = "terraform-state-rg"` to the `backend "azurerm"` configuration.

## Verification Results
### Automated Tests
- Ran `terraform init` successfully.

```
Terraform has been successfully initialized!

You may now begin working with Terraform. Try running "terraform plan" to see
any changes that are required for your infrastructure. All Terraform commands
should now work.
```
