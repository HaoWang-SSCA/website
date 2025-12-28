# Sunday Bulletin Feature Implementation Plan

## Summary

Implement bulletin hosting using **existing Azure Storage** (already used for audio files), with a QR code display on the home page and an admin upload page.

---

## Proposed Changes

### Infrastructure (Terraform)

#### [MODIFY] [main.tf](file:///c:/Projects/HaoWang-SSCA/website/infrastructure/main.tf)

Use existing storage account, add a new container for bulletins:

```hcl
# Rename existing storage account to general name if needed
# Add bulletin container to existing storage

resource "azurerm_storage_container" "bulletin" {
  name                  = "bulletin"
  storage_account_name  = azurerm_storage_account.main.name  # Use existing
  container_access_type = "blob"
}

output "bulletin_url" {
  value = "${azurerm_storage_account.main.primary_blob_endpoint}bulletin/SundayBulletin.pdf"
}
```

---

### Frontend - Hero Section

#### [MODIFY] [HeroSection.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Components/HeroSection.razor)

- Add bulletin download button with link to Azure storage
- Display QR code for bulletin link (using a QR code generator library or pre-generated image)

---

### Admin - Bulletin Upload Page

#### [NEW] [BulletinUpload.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/Admin/BulletinUpload.razor)

Admin page (authenticated only) to upload bulletin PDF:
- File input for PDF selection
- Upload button
- Progress indicator
- Success/error feedback

#### [NEW] [BulletinFunction.cs](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.API/Functions/BulletinFunction.cs)

API endpoint to handle bulletin upload:
- POST `/api/bulletin/upload` - Upload PDF to Azure Blob Storage
- Requires authentication

#### [MODIFY] [Header.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Shared/Header.razor)

Add navigation link "管理单张" (Manage Bulletin) for authenticated users.

---

## Verification Plan

1. **Infrastructure**: Apply Terraform changes, verify container created
2. **Upload**: Test PDF upload via admin page
3. **Display**: Verify bulletin link and QR code display on home page
4. **Download**: Confirm PDF downloads correctly from Azure storage
