# Resource Group
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

# Random suffix for globally unique names
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

# ============================================
# Azure Static Web App
# ============================================
resource "azurerm_static_web_app" "website" {
  name                = "${var.project_name}-swa-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  sku_tier            = var.static_web_app_sku
  sku_size            = var.static_web_app_sku

  app_settings = merge(
    {
      "ConnectionStrings__PostgreSQL"         = "Host=${var.postgres_host};Port=${var.postgres_port};Database=${var.postgres_database_name};Username=${var.postgres_username};Password=${var.postgres_password};SSL Mode=Require"
      "ConnectionStrings__AzureStorage"       = azurerm_storage_account.media.primary_connection_string
      "AzureStorage__BaseUrl"                 = trimsuffix(azurerm_storage_account.media.primary_blob_endpoint, "/")
      "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.main.instrumentation_key
      "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
    },
    # Add AAD settings only if provided
    var.aad_client_id != "" ? { "AAD_CLIENT_ID" = var.aad_client_id } : {},
    var.aad_client_secret != "" ? { "AAD_CLIENT_SECRET" = var.aad_client_secret } : {}
  )

  tags = var.tags
}

# ============================================
# PostgreSQL - Using Shared Server
# ============================================
# Application connects directly to the shared PostgreSQL server
# Database must already exist on the shared server

# ============================================
# Azure Storage Account (for media files)
# ============================================
resource "azurerm_storage_account" "media" {
  name                     = "${replace(var.project_name, "-", "")}media${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = var.location
  account_tier             = var.storage_account_tier
  account_replication_type = var.storage_replication_type

  blob_properties {
    cors_rule {
      allowed_headers    = ["*"]
      allowed_methods    = ["GET", "HEAD", "OPTIONS", "POST", "PUT"]
      allowed_origins    = ["*"] # Update with your domain in production
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }

  tags = var.tags
}

# Blob container for audio files
resource "azurerm_storage_container" "audio_files" {
  name                  = "audio-files"
  storage_account_name  = azurerm_storage_account.media.name
  container_access_type = "blob" # Public read access for audio files
}

# Blob container for bulletins
resource "azurerm_storage_container" "bulletin" {
  name                  = "bulletin"
  storage_account_name  = azurerm_storage_account.media.name
  container_access_type = "blob" # Public read access for bulletins
}

# ============================================
# Application Insights & Log Analytics
# ============================================
resource "azurerm_log_analytics_workspace" "main" {
  name                = "${var.project_name}-log-${random_string.suffix.result}"
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = var.tags
}

resource "azurerm_application_insights" "main" {
  name                = "${var.project_name}-insights-${random_string.suffix.result}"
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"

  tags = var.tags
}
