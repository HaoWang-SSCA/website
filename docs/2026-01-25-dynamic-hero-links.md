# Dynamic Hero Links Feature

**Date:** January 25, 2026  
**Version:** 1.0

## Overview

This feature adds the ability to display dynamic, manageable links on the hero section of the home page. Links can be external URLs or uploaded files, and they automatically disappear when reaching their expiry date.

## Features

### For Administrators
- Create, edit, and delete hero links from the management page
- Two link types supported:
  - **External URL** - Links to external websites (e.g., registration forms, event pages)
  - **File Upload** - Upload PDF, images, or documents that users can download
- Set expiry dates or choose **"Never Expires"** - links hide automatically when expired or stay visible indefinitely
- Control display order - determine which links appear first
- View all links including expired ones for reference

### For Users
- See dynamic announcement links prominently displayed on the home page
- Links open in new tabs for external URLs
- Direct file download/viewing for uploaded documents
- Automatic icon selection based on link type

## Technical Implementation

### Backend (API)

#### New Files Created

| File | Description |
|------|-------------|
| `src/SSCA.website.API/Models/HeroLink.cs` | Database entity model |
| `src/SSCA.website.API/Services/HeroLinkService.cs` | Business logic service |
| `src/SSCA.website.API/Functions/HeroLinksFunction.cs` | API endpoints |
| `src/SSCA.website.Shared/Models/HeroLinkDto.cs` | DTOs and request models |

#### Database Schema

**Table: `HeroLinks`**

| Column | Type | Description |
|--------|------|-------------|
| `Id` | GUID | Primary key |
| `Text` | VARCHAR(200) | Display text for the link |
| `ExternalUrl` | VARCHAR(500) | External URL (nullable) |
| `FileBlobName` | VARCHAR(500) | Uploaded file blob name (nullable) |
| `ExpiryDate` | DATETIME | When the link should stop displaying |
| `DisplayOrder` | INT | Sort order (lower = first) |
| `CreatedAt` | DATETIME | Creation timestamp |
| `UpdatedAt` | DATETIME | Last update timestamp |

#### API Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/hero-links` | Public | Get active (non-expired) links |
| `GET` | `/api/mgmt/hero-links` | Admin | Get all links including expired |
| `POST` | `/api/mgmt/hero-links` | Admin | Create a new link |
| `PUT` | `/api/mgmt/hero-links/{id}` | Admin | Update an existing link |
| `DELETE` | `/api/mgmt/hero-links/{id}` | Admin | Delete a link |
| `POST` | `/api/mgmt/hero-links/upload` | Admin | Upload a file for a link |
| `GET` | `/api/hero-links/files/{name}` | Public | Download/view uploaded file |

### Frontend (UI)

#### New Files Created

| File | Description |
|------|-------------|
| `src/SSCA.website.UI/Pages/Admin/HeroLinkManagement.razor` | Admin management page |

#### Modified Files

| File | Changes |
|------|---------|
| `src/SSCA.website.UI/Components/HeroSection.razor` | Added dynamic link display |

### Configuration Changes

| File | Changes |
|------|---------|
| `src/SSCA.website.API/Data/AppDbContext.cs` | Added HeroLinks DbSet |
| `src/SSCA.website.API/Program.cs` | Registered IHeroLinkService |

### Database Migration

A new migration `AddHeroLinks` was created to add the HeroLinks table.

## Usage Instructions

### Adding a New Link

1. Navigate to `/mgmt/hero-links` in the admin area
2. Click **"添加链接 Add Link"** button
3. Fill in the form:
   - **链接文字 (Link Text)**: The text displayed on the button
   - **链接类型 (Link Type)**: Choose between external URL or file upload
   - **过期日期 (Expiry Date)**: When the link should stop appearing
   - **显示顺序 (Display Order)**: Lower numbers appear first
4. Click **"保存 Save"**

### Editing a Link

1. Navigate to `/mgmt/hero-links`
2. Find the link you want to edit
3. Click **"编辑"** button
4. Make changes and click **"保存 Save"**

### Deleting a Link

1. Navigate to `/mgmt/hero-links`
2. Find the link you want to delete
3. Click **"删除"** button
4. Confirm deletion in the dialog

## Visual Design

Dynamic links are displayed with:
- **Amber/Orange gradient** background to stand out from other buttons
- **Slide-in animation** when the page loads
- **Appropriate icons** based on link type:
  - 📄 PDF files: `picture_as_pdf`
  - 📝 Registration links: `how_to_reg`
  - 📋 Form links: `edit_note`
  - 🔗 General external links: `open_in_new`
  - 📎 Other files: `attach_file`

## File Storage

Uploaded files are stored in Azure Blob Storage:
- Container: `hero-links`
- Blob naming pattern: `{yyyy-MM}/{guid}{extension}`

---

## Footer Updates

In addition to the hero links feature, the following footer updates were made:

### Contact Information Updated

| Field | Old Value | New Value |
|-------|-----------|-----------|
| Address | 123 Main Street, Vancouver, BC, V6A 1B2 | 17029 - 16th Avenue, Surrey, BC, Canada V3S 9M5 |
| Phone | (604) 555-1234 | 604-783-9158 |
| Email | info@sscabc.org | office@ssca-bc.org |
| Copyright | © 2024 SSCA-BC. All rights reserved. | © Copyright 2026 - All rights reserved 南素里基督教会 |

### Content Updates
- Organization name changed from "SSCA-BC" to "SSCA"
- Tagline updated to: "耶稣说：我就是道路、真理、生命，若不藉着我，没有人能到父那里去。"
- Quick Links simplified to only include "Contact Us"
- Social media links removed

---

## Deployment Notes

After deployment, the database migration will run automatically on startup. No manual database changes are required.

## Future Enhancements (Potential)

- Drag-and-drop reordering of links
- Analytics tracking for link clicks
- Scheduled activation (start date in addition to expiry date)
- Link categories or groups
- Custom icon selection
