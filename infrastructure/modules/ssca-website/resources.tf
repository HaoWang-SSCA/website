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

  # SWA app_settings: only AAD config (API settings are on the Function App now)
  app_settings = merge(
    var.aad_client_id != "" ? { "AAD_CLIENT_ID" = var.aad_client_id } : {},
    var.aad_client_secret != "" ? { "AAD_CLIENT_SECRET" = var.aad_client_secret } : {}
  )

  tags = var.tags
}

# ============================================
# Function App Runtime Storage
# ============================================
resource "azurerm_storage_account" "functions" {
  name                     = "${replace(var.project_name, "-", "")}func${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags                     = var.tags
}

# ============================================
# App Service Plan (for Function App)
# ============================================
resource "azurerm_service_plan" "functions" {
  name                = "${var.project_name}-plan-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.function_app_sku
  tags                = var.tags
}

# ============================================
# Azure Function App (Independent)
# ============================================
resource "azurerm_linux_function_app" "api" {
  name                       = "${var.project_name}-api-${random_string.suffix.result}"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = var.location
  service_plan_id            = azurerm_service_plan.functions.id
  storage_account_name       = azurerm_storage_account.functions.name
  storage_account_access_key = azurerm_storage_account.functions.primary_access_key

  site_config {
    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
    cors {
      allowed_origins = ["https://${azurerm_static_web_app.website.default_host_name}"]
    }
  }

  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME"              = "dotnet-isolated"
    "ConnectionStrings__PostgreSQL"         = "Host=${var.postgres_host};Port=${var.postgres_port};Database=${var.postgres_database_name};Username=${var.postgres_username};Password=${var.postgres_password};SSL Mode=Require"
    "ConnectionStrings__AzureStorage"       = azurerm_storage_account.media.primary_connection_string
    "AzureStorage__BaseUrl"                 = trimsuffix(azurerm_storage_account.media.primary_blob_endpoint, "/")
    "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.main.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
  }

  tags = var.tags

  lifecycle {
    ignore_changes = [
      # Azure may modify auth settings when linking to SWA
      auth_settings_v2,
    ]
  }
}

# ============================================
# Link Function App to Static Web App
# ============================================
resource "azurerm_static_web_app_function_app_registration" "api" {
  static_web_app_id = azurerm_static_web_app.website.id
  function_app_id   = azurerm_linux_function_app.api.id
}

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
      allowed_origins    = ["https://${azurerm_static_web_app.website.default_host_name}"]
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
  container_access_type = "blob"
}

# Blob container for bulletins
resource "azurerm_storage_container" "bulletin" {
  name                  = "bulletin"
  storage_account_name  = azurerm_storage_account.media.name
  container_access_type = "blob"
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
