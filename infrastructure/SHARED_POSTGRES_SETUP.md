# Shared PostgreSQL Server Setup

## Overview
The infrastructure has been modified to use a **shared PostgreSQL server** instead of creating a dedicated one per deployment. This reduces costs and simplifies management when multiple projects share the same database server.

## Changes Made

### 1. **resources.tf**
- **Removed**: 
  - `azurerm_postgresql_flexible_server` - No longer creating a new server
  - `azurerm_postgresql_flexible_server_firewall_rule` - Server firewall managed elsewhere
- **Modified**:
  - `azurerm_postgresql_flexible_server_database` - Now uses `var.postgres_server_id` instead of creating a server
  - Connection string in Static Web App now uses variables for host, port, username, and password

### 2. **variables.tf**
- **Removed**:
  - `postgres_sku`
  - `postgres_storage_mb`
  - `postgres_version`
  - `postgres_admin_username`
  - `postgres_admin_password`
  
- **Added**:
  - `postgres_server_id` - Resource ID of the existing shared server (required)
  - `postgres_host` - Hostname of the shared server (required)
  - `postgres_port` - Port number (default: 5432)
  - `postgres_database_name` - Database name to create (default: "ssca")
  - `postgres_username` - Username for authentication (required, sensitive)
  - `postgres_password` - Password for authentication (required, sensitive)

### 3. **outputs.tf**
- **Removed**:
  - `postgres_server_name`
  - `postgres_fqdn`
- **Updated**:
  - `postgres_connection_string` now uses input variables instead of server references

### 4. **terraform.tfvars.example**
- Updated to show configuration for shared server
- Include placeholders for `postgres_server_id`, `postgres_host`, etc.

## How to Use

### Prerequisites
You need access to an existing PostgreSQL Flexible Server with:
- Administrator credentials to create a new database
- Network access configured (firewall rules should allow Azure services)

### Configuration Steps

1. **Get the Shared Server Details**:
   ```bash
   # Get the resource ID
   az postgres flexible-server show -g <resource-group> -n <server-name> --query id
   
   # Get the FQDN
   az postgres flexible-server show -g <resource-group> -n <server-name> --query fullyQualifiedDomainName
   ```

2. **Update terraform.tfvars**:
   ```hcl
   postgres_server_id     = "/subscriptions/{id}/resourceGroups/{rg}/providers/Microsoft.DBforPostgreSQL/flexibleServers/{name}"
   postgres_host          = "your-postgres-server.postgres.database.azure.com"
   postgres_port          = 5432
   postgres_database_name = "ssca"
   ```

3. **Set Sensitive Variables** (using environment variables):
   ```bash
   export TF_VAR_postgres_username="your_username"
   export TF_VAR_postgres_password="your_password"
   ```

4. **Initialize and Apply**:
   ```bash
   terraform init
   terraform plan
   terraform apply
   ```

## Cost Benefits
- ✅ Single database server shared by multiple projects/environments
- ✅ No redundant server infrastructure costs
- ✅ Centralized backup and maintenance
- ✅ Easier to manage authentication and networking

## Migration from Dedicated Server

If you currently have a dedicated server, you can:

1. **Backup the existing database**:
   ```bash
   pg_dump -h old-server.postgres.database.azure.com -U olduser -d ssca > backup.sql
   ```

2. **Restore to shared server**:
   ```bash
   psql -h new-server.postgres.database.azure.com -U newuser -d ssca < backup.sql
   ```

3. **Update Terraform state** (if migrating):
   ```bash
   terraform state rm azurerm_postgresql_flexible_server.main
   terraform state rm azurerm_postgresql_flexible_server_firewall_rule.allow_azure
   ```

4. **Import the database** (optional, for state management):
   ```bash
   terraform import azurerm_postgresql_flexible_server_database.ssca <server-id>/databases/ssca
   ```

## Troubleshooting

- **Connection timeout**: Ensure firewall rules on the shared server allow your Azure services
- **Authentication failed**: Verify username/password are correct for the shared server
- **Database already exists**: The database name must be unique on the shared server, or the `azurerm_postgresql_flexible_server_database` resource should only be created once

## Support
For questions about the shared PostgreSQL server configuration, contact your database administrator.
