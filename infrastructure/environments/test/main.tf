# SSCA-BC Website - Test Environment

terraform {
  required_version = ">= 1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }

  backend "azurerm" {
    storage_account_name = "hwtfstate"
    container_name       = "tfstate"
    key                  = "ssca-website-test.tfstate"
    resource_group_name  = "terraform-state-rg"
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

provider "random" {}

# ============================================
# Variables (passed through to module)
# ============================================
variable "postgres_username" {
  description = "PostgreSQL username"
  type        = string
  sensitive   = true
}

variable "postgres_password" {
  description = "PostgreSQL password"
  type        = string
  sensitive   = true
}

variable "aad_client_id" {
  description = "Azure AD Client ID"
  type        = string
  sensitive   = true
  default     = ""
}

variable "aad_client_secret" {
  description = "Azure AD Client Secret"
  type        = string
  sensitive   = true
  default     = ""
}

# ============================================
# Module
# ============================================
module "ssca_website" {
  source = "../../modules/ssca-website"

  project_name        = "ssca-bc-test"
  environment         = "test"
  location            = "centralus"
  resource_group_name = "ssca-bc-test-rg"

  # PostgreSQL
  postgres_host          = "shared-data.postgres.database.azure.com"
  postgres_port          = 5432
  postgres_database_name = "ssca-test"

  postgres_username = var.postgres_username
  postgres_password = var.postgres_password

  # Storage
  storage_account_tier     = "Standard"
  storage_replication_type = "LRS"

  # Static Web App (Standard required for linked Function App backend)
  static_web_app_sku = "Standard"

  # Function App
  function_app_sku = "Y1"

  # Auth
  aad_client_id     = var.aad_client_id
  aad_client_secret = var.aad_client_secret

  # Tags
  tags = {
    Project     = "SSCA-BC Website"
    Environment = "Test"
    ManagedBy   = "Terraform"
    Owner       = "SSCA-BC"
  }
}

# ============================================
# Outputs (forward from module)
# ============================================
output "static_web_app_url" {
  value = module.ssca_website.static_web_app_url
}

output "static_web_app_api_key" {
  value     = module.ssca_website.static_web_app_api_key
  sensitive = true
}

output "function_app_name" {
  value = module.ssca_website.function_app_name
}

output "function_app_url" {
  value = module.ssca_website.function_app_url
}

output "storage_blob_endpoint" {
  value = module.ssca_website.storage_blob_endpoint
}

output "app_configuration" {
  value = module.ssca_website.app_configuration
}
