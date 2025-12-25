# Fix Azure Provisioning Error - Region Change

The provisioning failed because the subscription is restricted from creating PostgreSQL Flexible Servers in `westus2`. We need to switch to a different supported region. `centralus` is a supported alternative listed in [variables.tf](file:///c:/Projects/HaoWang-SSCA/website/infrastructure/variables.tf).

## User Review Required

> [!IMPORTANT]
> **Check [terraform.tfvars](file:///c:/Projects/HaoWang-SSCA/website/infrastructure/terraform.tfvars)**: This plan updates the default location in [variables.tf](file:///c:/Projects/HaoWang-SSCA/website/infrastructure/variables.tf). If you have `location` defined in your [terraform.tfvars](file:///c:/Projects/HaoWang-SSCA/website/infrastructure/terraform.tfvars) file (which I cannot read), you must update it to `"centralus"` manually, or this change will not take effect.

## Proposed Changes

### Infrastructure

#### [MODIFY] [variables.tf](file:///c:/Projects/HaoWang-SSCA/website/infrastructure/variables.tf)
- Update default `location` from `"westus2"` to `"centralus"`.

## Verification Plan

### Automated Tests
- Run `terraform plan` to verify the location change and ensure no layout conflicts.
- Since we are in the early stages (provisioning failed), `terraform apply` will be the ultimate test, but `plan` is strictly safe.

### Manual Verification
- Review `terraform plan` output to confirm `location` is set to `centralus` for:
    - `azurerm_resource_group.main`
    - `azurerm_postgresql_flexible_server.main`
    - `azurerm_static_web_app.website`
