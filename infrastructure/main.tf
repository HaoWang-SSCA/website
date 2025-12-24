# Terraform Configuration for SSCA-BC Website Infrastructure
# Resources: Azure Static Web App, PostgreSQL Flexible Server, Storage Account

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

  # Backend configuration for remote state
  # Works across tenants using Storage Account Key authentication
  # 
  # Setup:
  #   1. Run bootstrap-state.sh to create state storage
  #   2. Get key: az storage account keys list --account-name hwtfstate -o tsv --query '[0].value'
  #   3. Set env var: export ARM_ACCESS_KEY="<the_key>"
  #   4. Uncomment this block and run: terraform init
  #
  # backend "azurerm" {
  #   storage_account_name = "hwtfstate"
  #   container_name       = "tfstate"
  #   key                  = "ssca-website.tfstate"  # Unique per project
  #   # Uses ARM_ACCESS_KEY environment variable (don't hardcode!)
  # }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }

  # Uncomment and set if deploying to a different subscription than state storage
  # subscription_id = "PROJECT_SUBSCRIPTION_ID"
}

provider "random" {}
