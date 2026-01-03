# Alignment Plan: Modernize ssca-bc Functions Bootstrap

This plan modernizes the bootstrapping logic in the `ssca-bc` Functions project to use the SDK's latest `FunctionsApplication.CreateBuilder` pattern.

## Proposed Changes

### [ssca-bc/website]

#### [MODIFY] [Program.cs](file:///c:/Users/hwang5/Projects/ssca-bc/website/src/SSCA.website.API/Program.cs)
- Replace `HostBuilder` with `FunctionsApplication.CreateBuilder(args)`.
- Reorganize service registration to use the `builder.Services` pattern.
- Modernize the database migration logic to be part of the startup flow.

## Verification Plan

### Automated Tests
- Run `dotnet build src/SSCA.website.API/SSCA.website.API.csproj` to ensure cross-project compatibility.

### Manual Verification
- Verify that the API still correctly accesses the PostgreSQL database and Azure Blob Storage.
- Confirm that the `X-MS-CLIENT-PRINCIPAL` check (if added) correctly identifies the user.
