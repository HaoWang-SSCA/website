# Content Migration Plan: PHP to Blazor Website

This document outlines the content comparison between the original PHP website (`ssca-bc/ssca`) and the new Blazor website (`HaoWang-SSCA/website`), identifying missing content and proposing changes to bring the new site to feature parity.

## Summary

After comparing both codebases, the new Blazor site has implemented the core structure but is **missing several key content sections and features** from the original PHP site.

---

## Feature Comparison

| Feature | PHP Site | Blazor Site | Status |
|---------|----------|-------------|--------|
| Home Page Hero | ✅ Church branding, announcements | ✅ Generic content | 🟡 Needs Update |
| Faith/Hope/Mission Sections | ✅ 我们的信仰/盼望/使命 | ❌ Generic About | 🔴 Missing |
| Quick Links (Reading Materials, YouTube Live) | ✅ Implemented | ❌ Missing | 🔴 Missing |
| Sunday Bulletin Download | ✅ PDF download link | ✅ Button exists (not linked) | 🟡 Needs Link |
| Sunday Messages | ✅ With audio/video | ✅ Implemented | ✅ Done |
| Gospel Messages | ✅ With audio/video | ✅ Implemented | ✅ Done |
| Special Meetings | ✅ With audio/video | ✅ Implemented | ✅ Done |
| **Children Bible Stories** | ✅ 儿童圣经故事 audio list | ❌ Not implemented | 🔴 Missing |
| **Hymns Page** (赞美诗选) | ✅ Searchable hymns with lyrics | ❌ Not implemented | 🔴 Missing |
| Contact Form | ✅ Basic form | ✅ Enhanced form | ✅ Done |
| Google Maps | ✅ Embedded | ✅ Embedded | ✅ Done |
| Admin - Messages | ✅ CRUD for messages | ✅ MeetingList/Edit | ✅ Done |
| **Admin - Cell Groups** | ✅ Full management | ❌ Not implemented | 🔴 Missing |
| **Admin - Saints/Newcomers** | ✅ Member tracking | ❌ Not implemented | 🔴 Missing |
| **Admin - Usher** | ✅ Usher scheduling | ❌ Not implemented | 🔴 Missing |

---

## Proposed Changes

> [!IMPORTANT]
> The following changes are prioritized by impact. High-priority items affect the public-facing experience, while lower-priority admin features can be deferred.

### Proposed Changes

---

#### [MODIFY] [AboutSection.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Components/AboutSection.razor)

**Current:** Generic English content "About Our Organization..."

**Proposed:** Replace with the church's actual content from PHP site:
- 我们的信仰 (Our Faith) - Bible-centered faith statement
- 我们的盼望 (Our Hope) - Christ's return and the church as His bride  
- 我们的使命 (Our Mission) - Gospel proclamation and discipleship

Use a 3-column card layout similar to the PHP site's `features-area` section.

---

#### [MODIFY] [HeroSection.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Components/HeroSection.razor)

**Current:** Generic welcome message with non-functional buttons

**Proposed:**
- Update hero subtitle with church description
- Make "2025一年读经一遍" button link to `https://2025.getmanna.org/`
- Add link for Sunday Bulletin PDF (see separate bulletin hosting plan)
- Add YouTube live stream link: `https://www.youtube.com/@ssca9653/streams`

---

## Verification Plan

After implementing the changes:

1. **About Section**: Verify that the 3 feature cards (信仰/盼望/使命) display correctly with appropriate icons and Chinese text
2. **Hero Section**: Verify all buttons/links work:
   - "2025一年读经一遍" → Opens https://2025.getmanna.org/
   - "Sunday Bulletin" → Downloads/opens bulletin PDF
   - YouTube live stream link works
3. **Responsive Design**: Test all updates on mobile screen sizes

### Build Verification

```bash
cd c:\Projects\HaoWang-SSCA\website\src\SSCA.website.UI
dotnet build
```
