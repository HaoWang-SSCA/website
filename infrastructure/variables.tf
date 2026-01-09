# Input Variables for SSCA-BC Website Infrastructure

variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "ssca-bc"
}

variable "environment" {
  description = "Environment (dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "location" {
  description = "Azure region for resources (must support Azure Static Web Apps managed functions)"
  type        = string
  default     = "centralus" # Supported regions: westus2, centralus, eastus2, westeurope, eastasia
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "ssca-bc-rg"
}

# PostgreSQL Configuration - Shared Server
variable "postgres_host" {
  description = "Hostname of the shared PostgreSQL server"
  type        = string
}

variable "postgres_port" {
  description = "Port of the PostgreSQL server"
  type        = number
  default     = 5432
}

variable "postgres_database_name" {
  description = "Name of the database to create on the shared server"
  type        = string
  default     = "ssca"
}

variable "postgres_username" {
  description = "PostgreSQL username for the shared server"
  type        = string
  sensitive   = true
}

variable "postgres_password" {
  description = "PostgreSQL password for the shared server"
  type        = string
  sensitive   = true
}

# Storage Configuration
variable "storage_account_tier" {
  description = "Storage account tier"
  type        = string
  default     = "Standard"
}

variable "storage_replication_type" {
  description = "Storage replication type"
  type        = string
  default     = "LRS" # Locally redundant storage
}

# Static Web App Configuration
variable "static_web_app_sku" {
  description = "Azure Static Web App SKU"
  type        = string
  default     = "Free" # Free or Standard
}

variable "github_repo_url" {
  description = "GitHub repository URL"
  type        = string
  default     = "https://github.com/HaoWang-SSCA/website"
}

variable "github_branch" {
  description = "GitHub branch for deployment"
  type        = string
  default     = "main"
}

# Tags
variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    Project     = "SSCA-BC Website"
    Environment = "Production"
    ManagedBy   = "Terraform"
  }
}

# Azure AD Authentication (for Static Web App)
variable "aad_client_id" {
  description = "Azure AD Client ID for authentication"
  type        = string
  sensitive   = true
  default     = ""
}

variable "aad_client_secret" {
  description = "Azure AD Client Secret for authentication"
  type        = string
  sensitive   = true
  default     = ""
}
