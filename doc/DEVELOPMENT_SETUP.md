# Development Environment Setup

This document summarizes the required tools, configurations, and known constraints for developing the SSCA-BC website, specifically the backend API and database components.

## 1. Required SDKs & Runtimes

### .NET 8.0 (Mandatory)
*   **Version**: .NET 8.0 SDK (LTS)
*   **Why**: Azure Static Web Apps (SWA) "Managed Functions" currently supports .NET 8.0 isolated worker model. It does **not** yet support .NET 10.
*   **Constraint**: Even if you have .NET 10 installed locally, the projects (`SSCA.website.API`, `SSCA.website.Shared`) must target `net8.0`.
*   **Verification**:
    ```bash
    dotnet --list-sdks
    # Should show at least one 8.0.x version
    ```

## 2. Entity Framework Core Tools

### `dotnet-ef` Global Tool
*   **Version Rule**: The version of the `dotnet-ef` tool **must match** the major version of your project's target framework.
*   **Required Version**: `8.0.x`
*   **Common Error**: If you have the generic tool installed (which might default to the latest, e.g., 10.0), you will see `System.IO.FileNotFoundException` regarding missing runtimes when running migrations.
*   **Installation/Fix**:
    ```bash
    # uninstall incompatible version
    dotnet tool uninstall --global dotnet-ef

    # install specific version matching project (8.0)
    dotnet tool install --global dotnet-ef --version 8.0.11
    ```

### Database Migrations
*   **Strategy**: Code-First (C# classes define schema).
*   **Execution**:
    *   **Development**: Automigration on startup is enabled in `Program.cs`.
    *   **Production**: The same automigration logic applies when the Azure Function starts.
*   **Creating Migrations**:
    ```bash
    cd website/src/SSCA.website.API
    dotnet ef migrations add <MigrationName>
    ```

## 3. Azure Static Web Apps (SWA) Constraints

*   **API Model**: Managed Azure Functions (Isolated Worker).
*   **Configuration**:
    *   Defined in `staticwebapp.config.json` (routing).
    *   CI/CD via `.github/workflows/azure-static-web-apps.yml`.
*   **Platform**: The easy-auth and routing features of SWA are tightly integrated. Customizing the API routing requires careful synchronization with `staticwebapp.config.json`.

## 4. Useful Documentation Links

*   **Azure Static Web Apps**:
    *   [Configuration (staticwebapp.config.json)](https://learn.microsoft.com/en-us/azure/static-web-apps/configuration)
    *   [API Support (.NET Isolated)](https://learn.microsoft.com/en-us/azure/static-web-apps/apis-functions)
*   **Entity Framework Core**:
    *   [Tools Reference (dotnet-ef)](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
    *   [Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
*   **PostgreSQL**:
    *   [Npgsql Provider for EF Core](https://www.npgsql.org/efcore/)
*   **Infrastructure & Monitoring**:
    *   [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

## 4.5. CI/CD Authentication (Service Principal)
To enable Terraform deployment from GitHub Actions:
1.  **Create Service Principal**:
    ```bash
    # Run in Git Bash with MSYS_NO_PATHCONV to handle paths correctly
    MSYS_NO_PATHCONV=1 az ad sp create-for-rbac \
      --name "github-actions-ssca" \
      --role contributor \
      --scopes /subscriptions/<SUBSCRIPTION_ID> \
      --sdk-auth
    ```
2.  **Configure GitHub Secret**:
    *   Copy the JSON output.
    *   Go to GitHub Repo -> Settings -> Secrets and variables -> Actions.
    *   Create a New Repository Secret named `AZURE_CREDENTIALS` and paste the JSON.

## 4.6. CI/CD Secret Automation (Deployment Token)
The Terraform workflow is configured to **automatically update** the Static Web App deployment token (`AZURE_STATIC_WEB_APPS_API_TOKEN`) in GitHub Secrets.

**Requirement**:
You must provide a Personal Access Token (PAT) because the default `GITHUB_TOKEN` cannot update secrets.

1.  **Create Fine-grained PAT**:
    *   Go to **GitHub Settings** -> **Developer settings** -> **Personal access tokens** -> **Fine-grained tokens**.
    *   Select your repository (`HaoWang-SSCA/website`).
    *   **Permissions**:
        *   `Secrets`: **Read and Write**
        *   `Metadata`: Read-only (default)
2.  **Add Secret**:
    *   Name: `GH_PAT`
    *   Value: The token string starting with `github_pat_...`

## 5. Infrastructure & Deployment (Terraform)

### Azure Region Constraints
*   **PostgreSQL Flexible Server**: Some regions (like `westus2`) have restricted offer availability for certain subscription types.
*   **Recommendation**: Use `westus3` or `canadacentral` if you encounter "LocationIsOfferRestricted" errors.

### Terraform Backend
*   **Configuration**: When using the `azurerm` backend for state storage, you must explicitly define:
    *   `resource_group_name`
    *   `storage_account_name`
    *   `container_name`
    *   `key`
*   **Authentication**: Ensure `use_azuread_auth` or valid credentials (SAS Token/Access Key) are correctly set in your environment if not implicitly handled by logged-in CLI.

## 6. Pre-requisites
*   **Azure CLI**: Required for local testing, Terraform authentication, and manual deployment.
    *   Verification: `az version`
*   **Terraform**: Version 1.6+ recommended.

