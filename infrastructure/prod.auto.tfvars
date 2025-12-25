# Production Environment Configuration
# This file contains non-sensitive values and should be committed to git.

project_name        = "ssca-bc"
environment         = "prod"
location            = "centralus"
resource_group_name = "ssca-bc-website-rg"

# PostgreSQL
postgres_sku            = "B_Standard_B1ms"
postgres_storage_mb     = 32768
postgres_version        = "16"
postgres_admin_username = "sscaadmin"

# Storage
storage_account_tier     = "Standard"
storage_replication_type = "LRS"

# Static Web App
static_web_app_sku = "Free"
github_repo_url    = "https://github.com/HaoWang-SSCA/website"
github_branch      = "main"

# Tags
tags = {
  Project     = "SSCA-BC Website"
  Environment = "Production"
  ManagedBy   = "Terraform"
  Owner       = "SSCA-BC"
}
