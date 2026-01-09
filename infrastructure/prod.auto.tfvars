# Production Environment Configuration
# This file contains non-sensitive values and should be committed to git.

project_name        = "ssca-bc"
environment         = "prod"
location            = "centralus"
resource_group_name = "ssca-bc-website-rg"

# PostgreSQL - Shared Server Configuration
# Connection details for the shared PostgreSQL server
postgres_host          = "shared-data.postgres.database.azure.com"
postgres_port          = 5432
postgres_database_name = "wonner"

# Storage
storage_account_tier     = "Standard"
storage_replication_type = "LRS"

# Static Web App
static_web_app_sku = "Standard"
github_repo_url    = "https://github.com/HaoWang-SSCA/website"
github_branch      = "main"

# Tags
tags = {
  Project     = "SSCA-BC Website"
  Environment = "Production"
  ManagedBy   = "Terraform"
  Owner       = "SSCA-BC"
}
