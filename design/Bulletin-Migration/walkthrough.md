# SSCA Website Migration & Bulletin Feature Walkthrough

## 1. Public Content Migration

Migrated key public content from the PHP website to the new Blazor site.

### AboutSection.razor
Replaced generic English content with the church's actual 信仰/盼望/使命 content:
- **我们的信仰 (Our Faith)**
- **我们的盼望 (Our Hope)**
- **我们的使命 (Our Mission)**

Uses a 3-column responsive card layout with icons and custom CSS styling.

### HeroSection.razor
Updated with:
- Church's Chinese name and description.
- Working link to **2025一年读经一遍** (https://2025.getmanna.org/).
- Working link to **主日信息直播** (YouTube live stream).
- Original church background image (`hero-background.jpg`).

---

## 2. Sunday Bulletin Feature

Implemented a complete workflow for managing and displaying the weekly Sunday Bulletin.

### Infrastructure (Terraform)
- Renamed storage account from `audio` to `media` for general use.
- Added a dedicated `bulletin` blob container.
- Updated CORS rules to support authenticated uploads and public reads.

### API Implementation
- Created `FileStorageService` for reusable Azure Blob Storage logic.
- Implemented `BulletinFunction` (POST `/api/mgmt/bulletin-upload`) to handle PDF uploads.

### Admin Management
- Created `/mgmt/bulletin` page for authenticated users.
- Supports PDF selection, upload with progress feedback, and preview.
- Added navigation link "管理单张" in the header.

### Public Display (Hero Section)
- **Clickable QR Code**: The bulletin icon/button was removed and replaced by a high-visibility, clickable QR Code container.
- **Enhanced Visibility**: The QR code now sits on a solid, pure-white background with a distinct shadow, ensuring it doesn't look "greyed out" against the hero background image.
- **User Hint**: Added a clear "点击打开 PDF" (Click to open PDF) hint below the QR code for desktop users.
- **Mobile Friendly**: Congregants can scan the code with their mobile devices to instantly access the PDF.

---

## 3. Asset Migration

| File | Purpose | Target Location |
|------|---------|-----------------|
| `logo.png` | Church Logo | `wwwroot/images/logo.png` |
| `1.jpeg` | Hero Background | `wwwroot/images/hero-background.jpg` |
| `favicon.png` | Browser Icon | `wwwroot/favicon.png` |

---

## Verification

```
Build succeeded for both API and UI projects.
```

## Next Steps

1. **Deploy Infrastructure**: Run `terraform apply` to create the new `media` storage and `bulletin` container.
2. **Move Audio Files**: If you have existing files in the `audio` storage account, move them to the new `media` account's `audio-files` container.
3. **Set Configuration**: Ensure `AzureStorage:BaseUrl` is updated in Azure Static Web App settings if the generated name changes.
