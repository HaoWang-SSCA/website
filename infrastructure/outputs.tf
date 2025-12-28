# Output Values for SSCA-BC Website Infrastructure

# Resource Group
output "resource_group_name" {
  description = "The name of the resource group"
  value       = azurerm_resource_group.main.name
}

# Static Web App
output "static_web_app_name" {
  description = "The name of the Static Web App"
  value       = azurerm_static_web_app.website.name
}

output "static_web_app_url" {
  description = "The default URL of the Static Web App"
  value       = azurerm_static_web_app.website.default_host_name
}

output "static_web_app_api_key" {
  description = "The API key for Static Web App deployment"
  value       = azurerm_static_web_app.website.api_key
  sensitive   = true
}

# PostgreSQL
output "postgres_server_name" {
  description = "The name of the PostgreSQL server"
  value       = azurerm_postgresql_flexible_server.main.name
}

output "postgres_fqdn" {
  description = "The FQDN of the PostgreSQL server"
  value       = azurerm_postgresql_flexible_server.main.fqdn
}

output "postgres_connection_string" {
  description = "PostgreSQL connection string for application"
  value       = "Host=${azurerm_postgresql_flexible_server.main.fqdn};Database=ssca;Username=${var.postgres_admin_username};Password=${var.postgres_admin_password};SSL Mode=Require"
  sensitive   = true
}

# Storage Account
output "storage_account_name" {
  description = "The name of the storage account"
  value       = azurerm_storage_account.media.name
}

output "storage_account_connection_string" {
  description = "Storage account connection string"
  value       = azurerm_storage_account.media.primary_connection_string
  sensitive   = true
}

output "storage_blob_endpoint" {
  description = "Storage blob endpoint URL"
  value       = azurerm_storage_account.media.primary_blob_endpoint
}

output "audio_container_url" {
  description = "Audio files container URL"
  value       = "${azurerm_storage_account.media.primary_blob_endpoint}audio-files"
}

output "bulletin_container_url" {
  description = "Bulletin container URL"
  value       = "${azurerm_storage_account.media.primary_blob_endpoint}bulletin"
}

# App Configuration Summary
output "app_configuration" {
  description = "Configuration values needed for the application"
  value = {
    static_web_app_url = "https://${azurerm_static_web_app.website.default_host_name}"
    storage_base_url   = azurerm_storage_account.media.primary_blob_endpoint
  }
}
