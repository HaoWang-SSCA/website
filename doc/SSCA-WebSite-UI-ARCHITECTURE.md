# SSCA-BC Website UI - Architecture Document

## Project Overview

**Project Name:** SSCA-BC Website (South Surrey Christian Assembly 南素里基督教会)  
**Technology:** Blazor WebAssembly (.NET)  
**Project Path:** `website/src/SSCA.website.UI/`

## Summary of Changes

### Initial Development (November 2024)
1. **Created Blazor WebAssembly Project** - Built from HTML/CSS design reference
2. **Implemented Component Architecture** - Modular, reusable Razor components
3. **CSS Design System** - Custom vanilla CSS with variables, dark mode support, responsive breakpoints
4. **Chinese Localization** - Updated branding from English to Chinese (南素里基督教会)
5. **Removed Loading Spinner** - Simplified index.html startup experience
6. **Added .gitignore** - Exclude build artifacts (bin/, obj/, _framework/)

### Content Updates
- Header: "SSCA-BC" with "南素里基督教会" subtitle
- Navigation: Home, 主日信息, 福音聚会, 特别聚会, 联系我们
- Hero: "南素里基督教会" headline
- CTA Buttons: "2025一年读经一遍", "Sunday Bulletin 主日单张"

### Backend Updates (December 2024)
1. **Automatic Database Migration** - Configured `Program.cs` in the API project to automatically apply EF Core migrations on application startup. This ensures the database schema is always up-to-date without requiring manual CI/CD steps.
2. **Framework Compatibility** - Addressed .NET SDK version mismatches by targeting .NET 8.0 for compatibility with Azure Static Web Apps.

### Mobile Menu & Authentication (December 27, 2024)

#### Mobile Menu Feature
1. **Responsive Menu Button** - Added hamburger menu button that:
   - Hidden on desktop (≥768px) using `hidden-desktop` class
   - Visible on mobile (<768px)
   - Toggles between "menu" and "close" icons
2. **Mobile Menu Dropdown** - Slide-down menu panel with:
   - All navigation links (Home, 主日信息, 福音聚会, 特别聚会, 联系我们)
   - Smooth slide-down animation
   - Dark mode support
   - Closes when a link is clicked

#### Authentication UI
1. **Login/Logout Button** - Replaced static "登陆" button with dynamic authentication:
   - Shows "Login" button when not authenticated (links to `/.auth/login/aad`)
   - Shows username + "Logout" button when authenticated
2. **Admin Link** - Added "管理聚会" link in navigation:
   - Only visible when user is authenticated
   - Links to `/mgmt/meetings` admin page
   - Appears in both desktop and mobile menus
3. **Azure Static Web Apps Integration** - Uses `/.auth/me` endpoint to check authentication state on component initialization

#### New CSS Classes
- `.mobile-menu-btn` - Hamburger menu button styling
- `.mobile-menu` - Dropdown menu container with animation
- `.mobile-nav` - Vertical navigation links
- `.mobile-login-btn` - Full-width login button in mobile menu
- `.user-info` - Desktop user display (name + logout)
- `.user-name` - Username text styling
- `.mobile-user-info` - Mobile user display

---

## Architecture Diagram

```mermaid
graph TB
    subgraph "Blazor WebAssembly App"
        App[App.razor<br/>Router Configuration]
        App --> MainLayout[MainLayout.razor<br/>Page Structure]
        
        MainLayout --> Header[Header.razor<br/>Navigation & Branding]
        MainLayout --> Body[Page Content]
        MainLayout --> Footer[Footer.razor<br/>Links & Contact]
        
        subgraph "Pages"
            Home[Home.razor<br/>Landing Page]
            NotFound[NotFound.razor<br/>404 Page]
        end
        
        Body --> Home
        Body --> NotFound
        
        subgraph "Components"
            HeroSection[HeroSection.razor<br/>Hero Banner]
            AboutSection[AboutSection.razor<br/>About Content]
            NewsEventsSection[NewsEventsSection.razor<br/>News Grid]
            NewsCard[NewsCard.razor<br/>Reusable Card]
        end
        
        Home --> HeroSection
        Home --> AboutSection
        Home --> NewsEventsSection
        NewsEventsSection --> NewsCard
    end
    
    subgraph "Assets"
        CSS[app.css<br/>Design System]
        Fonts[Google Fonts<br/>Lexend]
        Icons[Material Symbols<br/>Outlined]
    end
    
    MainLayout -.-> CSS
    MainLayout -.-> Fonts
    MainLayout -.-> Icons
```

---

## Project Structure

```
SSCA.website.UI/
├── App.razor                 # Router & app entry point
├── Program.cs                # WebAssembly host configuration
├── _Imports.razor            # Global using statements
├── SSCA_BC.csproj            # Project file

├── Layout/
│   ├── MainLayout.razor      # Main page layout (Header + Content + Footer)
│   └── MainLayout.razor.css  # Scoped layout styles

├── Pages/
│   ├── Home.razor            # Landing page (/, composes sections)
│   └── NotFound.razor        # 404 error page

├── Shared/
│   ├── Header.razor          # Sticky header with nav & branding
│   └── Footer.razor          # Footer with links & contact info

├── Components/
│   ├── HeroSection.razor     # Hero banner with background & CTAs
│   ├── AboutSection.razor    # About organization section
│   ├── NewsCard.razor        # Reusable news/event card
│   └── NewsEventsSection.razor # Grid of news cards

└── wwwroot/
    ├── index.html            # HTML entry point
    └── css/
        └── app.css           # Complete CSS design system
```

---

## Component Details

### Layout Components

| Component | Purpose | Key Features |
|-----------|---------|--------------|
| **MainLayout** | Page wrapper | Flexbox structure, includes Header & Footer |
| **Header** | Navigation | Sticky header, logo, nav menu, login/logout, mobile menu, auth state |
| **Footer** | Site footer | 3-column grid, social links, contact info, copyright |

### Page Sections

| Component | Purpose | Parameters |
|-----------|---------|------------|
| **HeroSection** | Banner | Background image, headline, description, 2 CTA buttons |
| **AboutSection** | About | Centered text content, "Discover our story" link |
| **NewsEventsSection** | Content grid | Contains 3 NewsCard components |
| **NewsCard** | Reusable card | `ImageUrl`, `Category`, `Title`, `Description`, `Date`, `Link`, `LinkText`, `BadgeClass` |

---

## CSS Design System

### Color Palette
```css
--primary: #137fec      /* Blue - buttons, links, accents */
--accent: #FDB813       /* Gold - event badges */
--background-light: #f6f7f8
--background-dark: #101922
--slate-[50-900]        /* Grayscale range */
```

### Breakpoints
- **Mobile**: < 768px (single column, hidden nav)
- **Tablet**: 768px - 1023px (2-column grid)
- **Desktop**: ≥ 1024px (3-column grid, full nav)

### Key Features
- CSS custom properties for theming
- Dark mode support (`.dark` class)
- Responsive utilities (`.hidden-mobile`, `.hidden-desktop`)
- Smooth transitions and hover effects

---

## Dependencies

| Dependency | Purpose |
|------------|---------|
| **Blazor WebAssembly** | Client-side .NET runtime |
| **Lexend Font** | Google Fonts typography |
| **Material Symbols** | Icon set |

---

## Backend & Database

### Database Migrations
- **Strategy**: Code-first migrations using Entity Framework Core.
- **Automation**: The API (`SSCA.website.API`) is configured to run `context.Database.Migrate()` automatically during startup.
- **Benefits**:
  - Simplifies deployment (no separate pipeline step needed).
  - Ensures schema consistency across environments (Local & Azure).
  - Automatically handles First-Time-Run initialization.

### Logging & Monitoring
- **Application Insights**: The backend managed functions are linked to Azure Application Insights.
- **Log Source**: `azurerm_application_insights` resource in `infrastructure/resources.tf`.
- **Visibility**: Enables real-time logging, exception tracking, and performance monitoring for the API.

---

## Build & Run

```bash
cd website/src/SSCA.website.UI
dotnet build    # Compile project
dotnet run      # Start dev server at http://localhost:5227
```

---

## Future Enhancements

- [ ] Dark mode toggle button
- [x] ~~Functional mobile hamburger menu~~ ✅ Completed Dec 27, 2024
- [x] ~~Dynamic content from API/database~~ ✅ Meeting pages implemented
- [x] ~~Additional pages (主日信息, 福音聚会, etc.)~~ ✅ Completed
- [x] ~~User authentication (登陆 functionality)~~ ✅ Completed Dec 27, 2024
- [ ] Chinese/English language toggle
