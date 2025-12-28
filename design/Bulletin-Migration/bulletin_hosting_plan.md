# Sunday Bulletin PDF Hosting Plan

This document outlines options for hosting the Sunday Bulletin PDF in the new Blazor website.

---

## Current State

- **PHP Site:** The bulletin is hosted at `/bulletin/SundayBulletin.pdf` (static file)
- **Current file size:** ~2.5 MB
- **Update frequency:** Weekly (before each Sunday)

---

## Hosting Options Comparison

| Option | Pros | Cons | Recommended For |
|--------|------|------|-----------------|
| **1. Azure Blob Storage** | Easy updates via Azure Portal, CDN support, no redeployment needed | Requires Azure Storage account, small cost (~$0.02/GB/month) | ✅ Production |
| **2. Static wwwroot** | Simple, no extra infrastructure, free | Requires redeployment for each update | Small/infrequent changes |
| **3. Link to old site** | Zero effort now | Dependency on old infrastructure | Temporary only |

---

## Recommended: Azure Blob Storage

Since your project already uses Azure infrastructure (Azure Static Web Apps, PostgreSQL), adding Azure Blob Storage fits naturally.

### Benefits
- **No redeployment needed** - Upload new bulletin via Azure Portal or CLI
- **Global CDN** - Fast downloads worldwide via Azure CDN
- **Version history** - Optional blob versioning for archive
- **Scalable** - Handles any traffic volume

---

## Implementation Steps

### Step 1: Update Terraform Infrastructure

Add Azure Storage Account to your existing Terraform config:

```hcl
# In infrastructure/main.tf

resource "azurerm_storage_account" "bulletin" {
  name                     = "sscabulletin${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  
  blob_properties {
    cors_rule {
      allowed_headers    = ["*"]
      allowed_methods    = ["GET"]
      allowed_origins    = ["https://ssca-bc.org", "https://*.azurestaticapps.net"]
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }
}

resource "azurerm_storage_container" "public" {
  name                  = "public"
  storage_account_name  = azurerm_storage_account.bulletin.name
  container_access_type = "blob"
}

output "bulletin_storage_url" {
  value = "${azurerm_storage_account.bulletin.primary_blob_endpoint}public/"
}
```

### Step 2: Upload Initial Bulletin

```bash
# Upload bulletin via Azure CLI
az storage blob upload \
  --account-name <storage-account-name> \
  --container-name public \
  --name SundayBulletin.pdf \
  --file ./bulletin/SundayBulletin.pdf \
  --overwrite
```

### Step 3: Update HeroSection.razor

```razor
<a href="https://<storage-account>.blob.core.windows.net/public/SundayBulletin.pdf" 
   target="_blank" 
   class="btn btn-secondary btn-lg">
    <span>Sunday Bulletin 主日单张</span>
</a>
```

---

## Weekly Update Workflow

For whoever updates the bulletin each week:

**Option A: Azure Portal (easiest)**
1. Go to Azure Portal → Storage Account → Containers → public
2. Click Upload → Select new PDF → Enable "Overwrite if exists"
3. Done! Link stays the same.

**Option B: Azure CLI**
```bash
az storage blob upload --account-name sscabulletin --container-name public \
  --name SundayBulletin.pdf --file ./SundayBulletin.pdf --overwrite
```

---

## Alternative: Static wwwroot (Simpler but requires redeployment)

If Azure Blob Storage feels like overkill, you can use static files:

1. Place `SundayBulletin.pdf` in `src/SSCA.website.UI/wwwroot/bulletin/`
2. Reference as `/bulletin/SundayBulletin.pdf`
3. Commit and redeploy when bulletin changes

**Drawback:** Each weekly update requires a Git commit and CI/CD deployment.

---

## Cost Estimate (Azure Blob Storage)

| Item | Cost |
|------|------|
| Storage | ~$0.02/GB/month → $0.05/month for 2.5 MB |
| Bandwidth | First 100 GB/month free |
| **Total** | **< $1/month** |

---

## Recommendation

For a production church website with weekly bulletin updates, **Azure Blob Storage** is the best choice because:

1. Non-technical staff can update via Azure Portal
2. No Git/deployment knowledge required for updates
3. Minimal cost (~$1/month)
4. Consistent with existing Azure infrastructure
