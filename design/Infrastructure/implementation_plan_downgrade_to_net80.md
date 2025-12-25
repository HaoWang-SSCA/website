# Downgrade to .NET 8 for Azure Static Web Apps Compatibility

The current deployment is failing because Azure Static Web Apps (Managed Functions) does not support .NET 10. We must downgrade the entire solution to .NET 8 (LTS) to ensure compatibility.

## User Review Required

> [!IMPORTANT]
> **Downgrading Framework Version**
> We are changing the target framework from `net10.0` to `net8.0`. This might require code changes if you used any .NET 10 specific features, but given the project stage, this is unlikely.

## Proposed Changes

### Project Files

#### [MODIFY] [SSCA.website.API.csproj](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.API/SSCA.website.API.csproj)
- Change `TargetFramework` to `net8.0`.
- Downgrade dependencies:
    - `Microsoft.Azure.Functions.Worker` -> `1.21.0` (or compatible .NET 8 version)
    - `Microsoft.Azure.Functions.Worker.Sdk` -> `1.17.0`
    - `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` -> `1.2.0`
    - `Npgsql.EntityFrameworkCore.PostgreSQL` -> `8.0.x`

#### [MODIFY] [SSCA_BC.csproj](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/SSCA_BC.csproj)
- Change `TargetFramework` to `net8.0`.
- Downgrade `Microsoft.AspNetCore.Components.WebAssembly` dependencies to `8.0.x`.

#### [MODIFY] [SSCA.website.Shared.csproj](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.Shared/SSCA.website.Shared.csproj)
- Change `TargetFramework` to `net8.0`.

### Workflow

#### [MODIFY] [azure-static-web-apps.yml](file:///c:/Projects/HaoWang-SSCA/website/.github/workflows/azure-static-web-apps.yml)
- Update `dotnet-version` to `8.0.x`.
- Remove `--self-contained` and `-r linux-x64` from API build (let SWA handle the standard build if possible, or keep it standard).
- **Update**: Actually, standard SWA deployment for specific runtimes might prefer just `dotnet publish`. We will revert to a simpler build command compatible with the properly targeted framework.

## Verification Plan

### Automated Tests
1.  **Local Build**: Run `dotnet build` locally to ensure no compilation errors.
2.  **GitHub Action**: Push changes and monitor the deployment workflow.
