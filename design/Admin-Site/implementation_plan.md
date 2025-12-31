# Admin/Management Header and Styling Customization

This plan outlines the changes to create a distinct user experience for administrative pages.

## Proposed Changes

### UI Components

#### [NEW] [AdminLayout.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Layout/AdminLayout.razor)
- Create a new layout for admin pages.
- Uses `AdminHeader` instead of the public `Header`.

#### [NEW] [AdminHeader.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Shared/AdminHeader.razor)
- Dedicated header for admin pages.
- Links: 聚会 (Meetings), 主日单张 (Bulletin), 返回前台 (Public Site).
- Styled with `header-admin` class.

#### [NEW] [_Imports.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/Admin/_Imports.razor)
- Add `@layout AdminLayout` to apply the admin layout to all pages in the `Admin` folder.

#### [MODIFY] [Header.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Shared/Header.razor)
- Rename or keep as `PublicHeader`. For now, I'll simplify it to be the public header.
- Add **管理后台 (Admin)** link for authenticated users.

### Styles

#### [MODIFY] [app.css](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/wwwroot/css/app.css)
- Add styles for `.header-admin` to make it visually distinct.
- Proposed style:
  - Background color: A darker shade of blue or slate.
  - Distinct border.
  - "Admin" badge next to the logo.

## Verification Plan

### Automated Tests
- I will use the browser tool to navigate to:
  - `/` (Home) and verify the public header.
  - `/mgmt/meetings` and verify the admin header.
  - Expand mobile menu in both modes and verify links.

### Manual Verification
- Verify that clicking "返回前台" (Public Site) correctly returns the user to the public homepage with the normal header.
