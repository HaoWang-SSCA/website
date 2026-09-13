# Website Security and Sermon Platform Improvement Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Stabilize the SSCA website after the sermon metadata/PPT change by fixing P0 security issues first, then evolve “主日信息” into a maintainable sermon resource platform with normalized speakers, scriptures, attachments, and quality gates.

**Architecture:** Treat security and CI fixes as Phase 0 hardening with no database semantics change where possible. Then add EF Core-backed domain models (`Speaker`, `MeetingScriptureReference`, `MeetingAttachment`) behind compatibility-preserving migrations, while keeping existing public routes and fields working during rollout.

**Tech Stack:** .NET 8, C#, Blazor WebAssembly, Azure Functions isolated worker, EF Core/Npgsql/PostgreSQL, Azure Blob Storage, Azure Static Web Apps, GitHub Actions.

---

## Current Baseline

Repository: `HaoWang-SSCA/website`
Local path: `/home/azureuser/projects/website`
Default branch: `main`
Latest relevant commit: `07c5e47 feat: improve sermon metadata and resources`
Test deployment: GitHub Actions `Deploy Test Environment` succeeded for `07c5e47`.

Recent validated commands:

```bash
dotnet build src/SSCA.website.sln -c Release --no-restore
# Build succeeded, 0 errors, 1 vulnerability warning.

dotnet list src/SSCA.website.API/SSCA.website.API.csproj package --vulnerable --include-transitive
# Shows MailKit moderate, Microsoft.Extensions.Caching.Memory high, MimeKit moderate, Npgsql high.

dotnet list src/SSCA.website.UI/SSCA_BC.csproj package --vulnerable --include-transitive
# Shows System.Text.Json high advisories as transitive dependency.
```

Key risk found during review:

- `src/SSCA.website.API/Services/EmailService.cs` contains a hard-coded SMTP fallback password in Git history. Rotate the credential outside the repo before or during Task 1. Do not copy the current secret into any new file, issue, PR, or chat message.
- Admin Functions are `AuthorizationLevel.Anonymous` and rely primarily on Static Web Apps route protection.
- Current sermon metadata implementation works, but speaker/scripture/attachment data is still mostly free text.

---

## Execution Policy

1. Prefer PR branches for each phase unless Hao explicitly asks for direct `main` push.
2. No production deployment until Phase 0 security fixes are complete and Test is green.
3. Do not print, persist, or commit secrets. Use `[REDACTED]` in notes.
4. For EF tasks, generate migrations, read them manually, generate SQL script, then build/test.
5. Commit after each small, verified task.
6. If a task touches credentials or access policy, document required portal-side/manual actions separately from code changes.

---

## Phase 0 — Immediate Security and Release Hardening

### Task 0.1: Rotate SMTP Credential Outside Code

**Objective:** Invalidate the SMTP credential currently present in Git history before relying on code-only fixes.

**Files:**
- No code changes required in this task.
- Manual action in Microsoft 365 / mailbox admin portal or wherever the SMTP app password is managed.

**Steps:**

1. Rotate or revoke the SMTP password/app password for the mailbox currently used by the contact form.
2. Store the new value only in Azure Function App configuration or Key Vault.
3. Confirm Function App app settings include:
   - `Smtp__Host`
   - `Smtp__Port`
   - `Smtp__Username`
   - `Smtp__Password`
   - `Smtp__FromEmail`
   - `Smtp__FromName`
   - `ContactEmail`
4. Do not paste the new value into git, local docs, chat, or logs.

**Verification:**

- Old credential no longer works.
- Function App has new secret in app settings/Key Vault.
- Contact form still sends email in Test after Task 0.2 is deployed.

**Commit:** none.

---

### Task 0.2: Remove SMTP Secret Fallbacks from `EmailService`

**Objective:** Ensure code never contains or falls back to real SMTP credentials.

**Files:**
- Modify: `src/SSCA.website.API/Services/EmailService.cs`

**Implementation guidance:**

Replace credential fallback logic with explicit configuration reads. Host/port/from name may have safe defaults; username/password must not.

Target behavior:

```csharp
var smtpHost = _configuration["Smtp:Host"] ?? "smtp.office365.com";
var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
var smtpUsername = _configuration["Smtp:Username"];
var smtpPassword = _configuration["Smtp:Password"];
var fromEmail = _configuration["Smtp:FromEmail"] ?? smtpUsername;
var fromName = _configuration["Smtp:FromName"] ?? "SSCA-BC Website";
var recipientEmail = _configuration["ContactEmail"] ?? "tech@ssca-bc.org";

if (string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword))
{
    _logger.LogWarning("SMTP is not configured. Logging contact message instead of sending email.");
    LogContactMessage(request);
    return true;
}

if (string.IsNullOrWhiteSpace(fromEmail))
{
    fromEmail = smtpUsername;
}
```

**Verification:**

```bash
git diff --check
dotnet build src/SSCA.website.sln -c Release --no-restore
```

Also search for secret-like literals before committing:

```bash
git grep -n "Smtp:Password\|smtpPassword\|Password" src/SSCA.website.API/Services/EmailService.cs
```

Expected: no hard-coded password value; only configuration keys and variable names.

**Commit:**

```bash
git add src/SSCA.website.API/Services/EmailService.cs
git commit -m "fix: remove smtp credential fallback"
```

---

### Task 0.3: Add Secret Scanning to CI

**Objective:** Prevent future secret commits.

**Files:**
- Create: `.github/workflows/security-scan.yml` or modify a new PR validation workflow if introduced in Task 0.8.

**Implementation guidance:**

Use either Gitleaks or TruffleHog. Example with Gitleaks:

```yaml
name: Security Scan

on:
  pull_request:
  push:
    branches: [main, prod]

permissions:
  contents: read

jobs:
  gitleaks:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: gitleaks/gitleaks-action@v2
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**Verification:**

```bash
git diff --check
```

Push branch and confirm workflow starts.

**Commit:**

```bash
git add .github/workflows/security-scan.yml
git commit -m "ci: add secret scanning"
```

---

### Task 0.4: Add Server-Side Admin Authorization Service

**Objective:** Make `/api/mgmt/*` authorization enforceable in Functions code, not only in Static Web Apps routing.

**Files:**
- Create: `src/SSCA.website.API/Services/AdminAuthorizationService.cs`
- Modify: `src/SSCA.website.API/Program.cs`

**Implementation guidance:**

Create a small service that parses `x-ms-client-principal`. Validate either:

- role contains `admin`, or
- email/userDetails is in configured allowlist `Admin:AllowedEmails`.

Suggested interface:

```csharp
public interface IAdminAuthorizationService
{
    bool IsAdmin(HttpRequest request);
}
```

Implementation outline:

```csharp
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SSCA.website.API.Services;

public sealed class AdminAuthorizationService : IAdminAuthorizationService
{
    private readonly IConfiguration _configuration;

    public AdminAuthorizationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsAdmin(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("x-ms-client-principal", out var encoded) || string.IsNullOrWhiteSpace(encoded))
        {
            return false;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded!));
            var principal = JsonSerializer.Deserialize<ClientPrincipal>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (principal is null) return false;

            var roles = principal.UserRoles ?? [];
            if (roles.Any(r => string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var allowedEmails = (_configuration["Admin:AllowedEmails"] ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return allowedEmails.Any(email => string.Equals(email, principal.UserDetails, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private sealed class ClientPrincipal
    {
        public string? UserDetails { get; set; }
        public string[]? UserRoles { get; set; }
    }
}
```

Register in `Program.cs`:

```csharp
services.AddSingleton<IAdminAuthorizationService, AdminAuthorizationService>();
```

**Verification:**

```bash
dotnet build src/SSCA.website.sln -c Release --no-restore
```

**Commit:**

```bash
git add src/SSCA.website.API/Services/AdminAuthorizationService.cs src/SSCA.website.API/Program.cs
git commit -m "feat: add admin authorization service"
```

---

### Task 0.5: Apply Admin Authorization to Management Functions

**Objective:** Require admin authorization for every management endpoint, especially upload and mutation endpoints.

**Files:**
- Modify: `src/SSCA.website.API/Functions/AdminMeetingsFunction.cs`
- Search and modify any other `Route = "mgmt/..."` Function files.

**Steps:**

1. Inject `IAdminAuthorizationService` into each management Function class.
2. At the start of each management action, return 401/403 if not admin.
3. Protect or remove `AdminTest`.
4. Search all anonymous mgmt routes:

```bash
git grep -n 'Route = "mgmt/' src/SSCA.website.API
```

5. Apply the same guard to all matches.

Suggested helper inside a Function class:

```csharp
private IActionResult? RequireAdmin(HttpRequest req)
{
    return _adminAuthorization.IsAdmin(req)
        ? null
        : new UnauthorizedObjectResult("Admin authorization required.");
}
```

At top of each action:

```csharp
if (RequireAdmin(req) is { } unauthorized) return unauthorized;
```

**Verification:**

```bash
git diff --check
dotnet build src/SSCA.website.sln -c Release --no-restore
git grep -n 'Route = "mgmt/' src/SSCA.website.API
```

Manual Test after deploy:

- Anonymous request to `/api/mgmt/meetings` should fail.
- Logged-in admin request should pass.

**Commit:**

```bash
git add src/SSCA.website.API/Functions src/SSCA.website.API/Services src/SSCA.website.API/Program.cs
git commit -m "fix: enforce admin authorization on management api"
```

---

### Task 0.6: Tighten Static Web Apps Admin Roles

**Objective:** Align SWA route protection with server-side admin policy.

**Files:**
- Modify: `src/SSCA.website.UI/wwwroot/staticwebapp.config.json`

**Implementation guidance:**

Change `/mgmt/*` and `/api/mgmt/*` from:

```json
"allowedRoles": ["authenticated"]
```

to:

```json
"allowedRoles": ["admin"]
```

Document required SWA invitation/role assignment for admin users.

**Verification:**

```bash
python3 -m json.tool src/SSCA.website.UI/wwwroot/staticwebapp.config.json >/tmp/staticwebapp.config.validated.json
dotnet build src/SSCA.website.sln -c Release --no-restore
```

**Commit:**

```bash
git add src/SSCA.website.UI/wwwroot/staticwebapp.config.json
git commit -m "fix: require admin role for management routes"
```

---

### Task 0.7: Upgrade Vulnerable NuGet Dependencies

**Objective:** Remove known High vulnerabilities and current MailKit/MimeKit warnings.

**Files:**
- Modify: relevant `.csproj` files under `src/`.

**Steps:**

1. Inspect current package references:

```bash
dotnet list src/SSCA.website.sln package --include-transitive
```

2. Upgrade top-level packages to compatible patched versions. Likely candidates:
   - `Npgsql.EntityFrameworkCore.PostgreSQL`
   - `Microsoft.Extensions.Caching.Memory`
   - `System.Text.Json` if needed as direct package override
   - `MailKit` / `MimeKit`

3. Restore and check vulnerabilities:

```bash
dotnet restore src/SSCA.website.sln
dotnet list src/SSCA.website.API/SSCA.website.API.csproj package --vulnerable --include-transitive
dotnet list src/SSCA.website.UI/SSCA_BC.csproj package --vulnerable --include-transitive
```

Expected: no High vulnerabilities remain. If Moderate remains due to ecosystem constraints, document it.

4. Build:

```bash
dotnet build src/SSCA.website.sln -c Release --no-restore
```

**Commit:**

```bash
git add src/**/*.csproj
git commit -m "chore: upgrade vulnerable dotnet packages"
```

---

### Task 0.8: Add Pull Request Quality Gate

**Objective:** Ensure future changes are checked before merge/deploy.

**Files:**
- Create: `.github/workflows/pr-validation.yml`

**Workflow should run on:**

```yaml
on:
  pull_request:
    branches: [main, prod]
```

**Checks:**

- `dotnet restore src/SSCA.website.sln`
- `dotnet build src/SSCA.website.sln -c Release --no-restore`
- `dotnet test src/SSCA.website.sln -c Release --no-build`
- `dotnet list ... package --vulnerable --include-transitive`
- `dotnet format --verify-no-changes` once formatting baseline is fixed
- secret scan, if not separate

**Verification:**

Push branch and open PR. Confirm the PR workflow runs and fails/passes as expected.

**Commit:**

```bash
git add .github/workflows/pr-validation.yml
git commit -m "ci: add pull request validation"
```

---

## Phase 1 — Sermon Domain Model Foundation

### Task 1.1: Add Real Test Projects

**Objective:** Stop treating `dotnet test` as meaningful without tests.

**Files:**
- Create: `src/SSCA.website.API.Tests/SSCA.website.API.Tests.csproj`
- Create: `src/SSCA.website.API.Tests/...`
- Modify: `src/SSCA.website.sln`

**Steps:**

1. Create xUnit test project.
2. Reference `SSCA.website.API` and `SSCA.website.Shared`.
3. Add one smoke test for speaker cleaning/sort behavior after extracting those helpers to a testable service in Task 1.2.
4. Add to solution.

**Commands:**

```bash
dotnet new xunit -n SSCA.website.API.Tests -o src/SSCA.website.API.Tests
dotnet sln src/SSCA.website.sln add src/SSCA.website.API.Tests/SSCA.website.API.Tests.csproj
dotnet add src/SSCA.website.API.Tests/SSCA.website.API.Tests.csproj reference src/SSCA.website.API/SSCA.website.API.csproj
dotnet test src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src/SSCA.website.sln src/SSCA.website.API.Tests
git commit -m "test: add api test project"
```

---

### Task 1.2: Extract Speaker Normalization into a Testable Service

**Objective:** Keep current behavior but stop hiding it inside `MeetingService` static helpers.

**Files:**
- Create: `src/SSCA.website.API/Services/SpeakerNameService.cs`
- Modify: `src/SSCA.website.API/Services/MeetingService.cs`
- Modify: `src/SSCA.website.API/Program.cs`
- Test: `src/SSCA.website.API.Tests/SpeakerNameServiceTests.cs`

**Acceptance criteria:**

- “王弟兄” displays/sorts as “王”.
- “Bro. John Wang” displays/sorts as “John Wang”.
- A Chinese name without English can produce a pinyin-based sort key.
- Existing public API output remains compatible.

**Verification:**

```bash
dotnet test src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src
git commit -m "refactor: extract speaker name normalization"
```

---

### Task 1.3: Add `Speaker` Entity and Migration

**Objective:** Introduce a durable speaker catalog without breaking existing sermon records.

**Files:**
- Create: `src/SSCA.website.API/Models/Speaker.cs`
- Modify: `src/SSCA.website.API/Models/MessageMeeting.cs`
- Modify: `src/SSCA.website.API/Data/AppDbContext.cs`
- Migration: `src/SSCA.website.API/Migrations/*AddSpeakers.cs`
- Shared DTO/request changes as needed.

**Model outline:**

```csharp
public class Speaker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string? ChineseName { get; set; }
    public string? EnglishName { get; set; }
    public string? PinyinName { get; set; }
    public string SortName { get; set; } = string.Empty;
    public string? Aliases { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

`MessageMeeting` adds nullable `SpeakerId` first to allow safe backfill.

**Migration safety:**

1. Create `Speakers` table.
2. Add nullable `SpeakerId` to `MessageMeetings`.
3. Do not drop `MessageMeetings.Speaker` yet.
4. Optionally backfill in code task after initial deploy, or in migration with conservative distinct values.

**Verification:**

```bash
/tmp/dotnet-ef-tools/dotnet-ef migrations add AddSpeakers --project src/SSCA.website.API/SSCA.website.API.csproj --startup-project src/SSCA.website.API/SSCA.website.API.csproj
/tmp/dotnet-ef-tools/dotnet-ef migrations script <PreviousMigration> AddSpeakers --project src/SSCA.website.API/SSCA.website.API.csproj --startup-project src/SSCA.website.API/SSCA.website.API.csproj
dotnet build src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src/SSCA.website.API src/SSCA.website.Shared
git commit -m "feat: add speaker catalog model"
```

---

### Task 1.4: Add Speaker Admin and Backfill/Merge Flow

**Objective:** Let admins normalize historical speaker names and prevent new duplicates.

**Files:**
- Create/modify API management endpoints under `src/SSCA.website.API/Functions/`
- Create UI page: `src/SSCA.website.UI/Pages/Admin/Speakers.razor`
- Modify navigation if needed.

**Acceptance criteria:**

- Admin can list speakers.
- Admin can create/edit speaker display, English name, Chinese name, pinyin/sort name.
- Admin can merge duplicate aliases or map an existing raw `MessageMeeting.Speaker` to a speaker.
- Meeting edit page can select existing speaker and still preserve legacy text during transition.

**Verification:**

```bash
dotnet build src/SSCA.website.sln -c Release
dotnet test src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src
git commit -m "feat: add speaker management"
```

---

## Phase 2 — Structured Scripture Search

### Task 2.1: Add Scripture Parser Service and Tests

**Objective:** Parse common Chinese/English scripture inputs into normalized references.

**Files:**
- Create: `src/SSCA.website.API/Services/ScriptureParser.cs`
- Test: `src/SSCA.website.API.Tests/ScriptureParserTests.cs`

**Acceptance examples:**

- `约翰福音 3:16` -> `BookCode=John`, `StartChapter=3`, `StartVerse=16`
- `约3:16-18` -> John 3:16-18
- `John 3:16` -> John 3:16
- `创 1:1-2` -> Genesis 1:1-2

**Verification:**

```bash
dotnet test src/SSCA.website.sln -c Release --filter ScriptureParserTests
```

**Commit:**

```bash
git add src
git commit -m "feat: add scripture parser"
```

---

### Task 2.2: Add `MeetingScriptureReference` Entity and Migration

**Objective:** Store searchable scripture references separately from display text.

**Files:**
- Create: `src/SSCA.website.API/Models/MeetingScriptureReference.cs`
- Modify: `src/SSCA.website.API/Models/MessageMeeting.cs`
- Modify: `src/SSCA.website.API/Data/AppDbContext.cs`
- Migration: `*AddMeetingScriptureReferences.cs`

**Model outline:**

```csharp
public class MeetingScriptureReference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public MessageMeeting Meeting { get; set; } = null!;
    public string BookCode { get; set; } = string.Empty;
    public int BookOrder { get; set; }
    public int StartChapter { get; set; }
    public int? StartVerse { get; set; }
    public int? EndChapter { get; set; }
    public int? EndVerse { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
```

**Indexes:**

- `(BookCode, StartChapter)`
- `MeetingId`

**Verification:**

```bash
/tmp/dotnet-ef-tools/dotnet-ef migrations add AddMeetingScriptureReferences --project src/SSCA.website.API/SSCA.website.API.csproj --startup-project src/SSCA.website.API/SSCA.website.API.csproj
dotnet build src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src/SSCA.website.API
git commit -m "feat: add structured scripture references"
```

---

### Task 2.3: Wire Scripture Save/Search

**Objective:** Populate structured references on create/update and search by normalized scripture.

**Files:**
- Modify: `src/SSCA.website.API/Services/MeetingService.cs`
- Modify: request/DTO models as needed.
- Modify UI pages only if new query fields are added.

**Acceptance criteria:**

- Saving a meeting with parseable `Scripture` creates references.
- Updating `Scripture` replaces references for that meeting.
- Searching `约3:16` matches `约翰福音 3:16`.
- Existing `Scripture.Contains` can remain as fallback during transition.

**Verification:**

```bash
dotnet test src/SSCA.website.sln -c Release
dotnet build src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src
git commit -m "feat: search sermons by structured scripture"
```

---

## Phase 3 — Attachments and Blob Lifecycle

### Task 3.1: Add `MeetingAttachment` Entity

**Objective:** Replace one-off audio/PPT fields with a general attachment model while preserving compatibility.

**Files:**
- Create: `src/SSCA.website.API/Models/MeetingAttachment.cs`
- Modify: `src/SSCA.website.API/Data/AppDbContext.cs`
- Migration: `*AddMeetingAttachments.cs`

**Model outline:**

```csharp
public class MeetingAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public MessageMeeting Meeting { get; set; } = null!;
    public string Type { get; set; } = string.Empty; // Audio, PowerPoint, Pdf, Handout, Transcript, Other
    public string BlobName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Status { get; set; } = "Attached";
    public bool IsPublic { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? UploadedBy { get; set; }
}
```

**Verification:**

```bash
dotnet build src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src/SSCA.website.API
git commit -m "feat: add meeting attachment model"
```

---

### Task 3.2: Add Attachment Upload/Download API

**Objective:** Centralize upload validation and controlled download for audio/PPT/PDF resources.

**Files:**
- Create or modify: `src/SSCA.website.API/Functions/AdminAttachmentsFunction.cs`
- Create or modify: `src/SSCA.website.API/Functions/MeetingAttachmentsFunction.cs`
- Modify: `src/SSCA.website.API/Services/FileStorageService.cs`

**Requirements:**

- Admin upload requires `IAdminAuthorizationService`.
- Validate extension + MIME + size.
- Store original filename as metadata, not as trusted blob path.
- Public download by meeting/attachment ID validates `IsPublic` and returns correct content type/disposition.
- Prefer private blob containers + Function streaming or SAS.

**Verification:**

```bash
dotnet build src/SSCA.website.sln -c Release
dotnet test src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src
git commit -m "feat: add controlled meeting attachment downloads"
```

---

### Task 3.3: Add Attachment UI and Orphan Cleanup

**Objective:** Let admins manage attachments and avoid orphan blob accumulation.

**Files:**
- Modify: `src/SSCA.website.UI/Pages/Admin/MeetingEdit.razor`
- Modify: public meeting pages.
- Optional create cleanup service/job if project has scheduled Functions pattern.

**Acceptance criteria:**

- Admin can add/remove/replace attachments.
- Existing PPT/audio display still works during migration.
- Public pages render a unified “资料” section.
- Pending uploads not attached to a meeting can be cleaned after a configured age.

**Verification:**

```bash
dotnet build src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src
git commit -m "feat: manage sermon attachments"
```

---

## Phase 4 — Search UX and Data Quality

### Task 4.1: Persist Search Filters in URL

**Objective:** Make sermon search results shareable and refresh-safe.

**Files:**
- Modify: `src/SSCA.website.UI/Pages/SundayMessages.razor`
- Modify: `src/SSCA.website.UI/Pages/GospelMeetings.razor`
- Modify: `src/SSCA.website.UI/Pages/SpecialMeetings.razor`

**Acceptance criteria:**

- Speaker/topic/scripture/date filters appear in URL query string.
- Page reload restores filters.
- Clear filters updates URL.

**Verification:**

```bash
dotnet build src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src/SSCA.website.UI/Pages
git commit -m "feat: persist sermon search filters in url"
```

---

### Task 4.2: Add Admin Data Quality Panel

**Objective:** Help admins find missing or unnormalized sermon data.

**Files:**
- Create: `src/SSCA.website.UI/Pages/Admin/MeetingDataQuality.razor`
- Add API endpoint under `src/SSCA.website.API/Functions/`

**Checks:**

- Missing scripture.
- Speaker not linked to `SpeakerId`.
- Attachment blob missing or orphaned.
- Gospel/Special both true, if still possible.

**Verification:**

```bash
dotnet build src/SSCA.website.sln -c Release
```

**Commit:**

```bash
git add src
git commit -m "feat: add sermon data quality dashboard"
```

---

## Phase 5 — Production Release Safety

### Task 5.1: Split Production Database Migration from App Startup

**Objective:** Avoid production schema changes during Function cold start.

**Files:**
- Modify: `src/SSCA.website.API/Program.cs`
- Modify: `.github/workflows/deploy-prod.yml`
- Possibly create migration script workflow.

**Plan:**

- Keep startup migrate for Test only if desired.
- For Prod, generate idempotent SQL with EF tooling.
- Require environment approval before applying SQL.
- Backup DB before migration.
- Deploy app only after migration succeeds.

**Verification:**

- Test environment still migrates or migration job succeeds.
- Prod workflow requires manual approval.

**Commit:**

```bash
git add src/SSCA.website.API/Program.cs .github/workflows
git commit -m "ci: separate production database migrations"
```

---

### Task 5.2: Harden Deployment Workflows

**Objective:** Reduce accidental production deployments and infra drift.

**Files:**
- Modify: `.github/workflows/*.yml`

**Actions:**

- Add `concurrency` to deploy workflows.
- Require manual approval/environment protection for prod.
- Avoid `terraform apply -auto-approve` on direct branch push.
- Use explicit Function App name from env/Terraform output, not first item from `az functionapp list`.
- Add least-privilege `permissions` to workflows.

**Verification:**

```bash
git diff --check
```

Push to branch and inspect workflow syntax in GitHub.

**Commit:**

```bash
git add .github/workflows
git commit -m "ci: harden deployment workflows"
```

---

## Recommended Execution Order

1. Phase 0, Tasks 0.1-0.8.
2. Deploy Phase 0 to Test and verify admin/contact form behavior.
3. Decide whether to push Phase 0 to Prod before data model work.
4. Phase 1 Speaker model.
5. Phase 2 Scripture model/search.
6. Phase 3 Attachment model/blob lifecycle.
7. Phase 4 UX/data quality.
8. Phase 5 production migration/deploy hardening if not already completed during Phase 0.

## Definition of Done for Each Phase

- `git diff --check` passes.
- `dotnet restore src/SSCA.website.sln` passes.
- `dotnet build src/SSCA.website.sln -c Release` passes.
- `dotnet test src/SSCA.website.sln -c Release` passes; if no test covers the change, explicitly add or document missing coverage.
- EF migration script is generated and reviewed for schema tasks.
- GitHub Actions on pushed branch/main are green.
- No secrets appear in diffs or logs.
