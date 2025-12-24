# Message Meeting (讲道信息) Feature Plan

## Overview

Add a Message Meeting data feature to the SSCA-BC church website to display sermons/meeting recordings. This involves creating a new entity, admin CRUD pages, and three public listing pages filtered by meeting type.

---

## User Review Required

**Confirmed Decisions:**
- ✅ **Backend**: Azure Functions (integrated with Static Web Apps)
- ✅ **Database**: PostgreSQL with EF Core
- ✅ **Audio Storage**: Azure Blob Storage

---

## 1. Entity Design

### MessageMeeting Entity
```csharp
public class MessageMeeting
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }              // 信息日期
    public string Speaker { get; set; }             // 讲员
    public string Topic { get; set; }               // 信息主题
    public string? AudioBlobName { get; set; }      // Audio file in Azure Blob Storage
    public string? VideoUrl { get; set; }           // YouTube/Video link
    public bool IsGospel { get; set; }              // 福音信息 flag
    public bool IsSpecialMeeting { get; set; }      // 特别聚会 flag
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Audio File Storage (Azure Blob Storage)

| Component | Details |
|-----------|--------|
| **Container** | `audio-files` |
| **Naming** | `{year}/{guid}-{filename}.mp3` |
| **Access** | Public read via CDN or SAS tokens |
| **Upload** | Admin uploads via API → Blob Storage |
| **URL Format** | `https://{storage}.blob.core.windows.net/audio-files/{blobName}` |

### Page Filtering + Pagination + Search
| Page | Filter | Search Fields |
|------|--------|---------------|
| **主日信息** | `!IsGospel && !IsSpecialMeeting` | Speaker, Topic, Date range |
| **福音聚会** | `IsGospel == true` | Speaker, Topic, Date range |
| **特别聚会** | `IsSpecialMeeting == true` | Speaker, Topic, Date range |

**Pagination**: 10 items per page, with page navigation controls

---

## 2. Proposed Architecture

```mermaid
graph TB
    subgraph "Azure Static Web Apps"
        UI[Blazor WASM UI<br/>SSCA.website.UI]
    end
    
    subgraph "Azure Functions (API)"
        API[SSCA.website.API<br/>Azure Functions v4]
        API --> Public[Public Endpoints]
        API --> Admin[Admin Endpoints]
    end
    
    subgraph "Database"
        DB[(PostgreSQL<br/>Azure Database)]
    end
    
    UI -->|HTTP| API
    API -->|EF Core| DB
    
    Public --> GetSundayMessages[GET /api/meetings/sunday]
    Public --> GetGospelMeetings[GET /api/meetings/gospel]
    Public --> GetSpecialMeetings[GET /api/meetings/special]
    
    Admin --> CRUD[GET/POST/PUT/DELETE<br/>/api/admin/meetings]
```

---

## 3. Proposed Changes

### New Projects

#### [NEW] SSCA.website.API (Azure Functions)
An Azure Functions project for the backend API.

| File | Purpose |
|------|---------|
| `Functions/MeetingsFunction.cs` | Public API endpoints for meeting lists |
| `Functions/AdminMeetingsFunction.cs` | Admin CRUD endpoints (authenticated) |
| `Models/MessageMeeting.cs` | Entity model |
| `Data/AppDbContext.cs` | EF Core DbContext |
| `Services/MeetingService.cs` | Business logic |

#### [NEW] SSCA.website.Shared
Shared library for DTOs and models between UI and API.

| File | Purpose |
|------|---------|
| `Models/MessageMeetingDto.cs` | DTO for API responses |
| `Models/CreateMeetingRequest.cs` | Admin create request |
| `Models/UpdateMeetingRequest.cs` | Admin update request |

---

### Modify Blazor UI (SSCA.website.UI)

#### [NEW] Pages/SundayMessages.razor
Display 主日信息 (Sunday Messages) - filtered by `!IsGospel && !IsSpecialMeeting`

#### [NEW] Pages/GospelMeetings.razor  
Display 福音聚会 (Gospel Meetings) - filtered by `IsGospel == true`

#### [NEW] Pages/SpecialMeetings.razor
Display 特别聚会 (Special Meetings) - filtered by `IsSpecialMeeting == true`

#### [NEW] Pages/Admin/MeetingList.razor
Admin page to list all meetings with edit/delete actions

#### [NEW] Pages/Admin/MeetingEdit.razor
Admin page to create/edit meeting with form for all attributes

#### [MODIFY] [Header.razor](file:///c:/Users/hwang5/Projects/ssca-bc/website/src/SSCA.website.UI/Shared/Header.razor)
Update navigation links to point to new pages

---

## 4. Database Setup (PostgreSQL)

### EF Core Configuration
```csharp
// Program.cs / Startup.cs
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(Configuration.GetConnectionString("PostgreSQL")));
```

### Connection String
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=your-server.postgres.database.azure.com;Database=ssca;Username=admin;Password=..."
  }
}
```

### Data Migration (Future)
When migrating existing data from other sources:
1. Use EF Core migrations for schema management
2. Create data import scripts or use Azure Data Factory
3. Validate data integrity after migration

---

## 5. Azure Static Web Apps + Functions Integration

Azure Static Web Apps has **built-in API support** via Azure Functions:

```
/website
├── src/
│   ├── SSCA.website.UI/          # Blazor WASM (deployed to static content)
│   └── SSCA.website.API/         # Azure Functions (deployed as managed API)
├── staticwebapp.config.json      # SWA routing config
```

### Benefits:
- **Automatic deployment** - Both UI and API deploy together
- **Same domain** - API available at `/api/*` (no CORS issues)
- **Authentication** - Built-in auth with Azure AD, GitHub, etc.
- **Staging environments** - Automatic per-PR staging slots

---

## 6. API Endpoints Design

### Public Endpoints (No Auth)
| Method | Endpoint | Query Params | Description |
|--------|----------|--------------|-------------|
| GET | `/api/meetings/sunday` | `?page=1&pageSize=10&speaker=&topic=&dateFrom=&dateTo=` | List Sunday messages with pagination/search |
| GET | `/api/meetings/gospel` | `?page=1&pageSize=10&speaker=&topic=&dateFrom=&dateTo=` | List Gospel meetings with pagination/search |
| GET | `/api/meetings/special` | `?page=1&pageSize=10&speaker=&topic=&dateFrom=&dateTo=` | List Special meetings with pagination/search |
| GET | `/api/meetings/{id}` | - | Get single meeting details |

### Admin Endpoints (Requires Auth)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/meetings` | List all meetings (paginated) |
| POST | `/api/admin/meetings` | Create meeting |
| POST | `/api/admin/meetings/{id}/audio` | Upload audio file to Blob Storage |
| PUT | `/api/admin/meetings/{id}` | Update meeting |
| DELETE | `/api/admin/meetings/{id}` | Delete meeting |

---

## 7. Technology Stack Summary

| Component | Technology |
|-----------|------------|
| **Frontend** | Blazor WebAssembly (.NET 10) |
| **Backend** | Azure Functions v4 (C#) |
| **Database** | PostgreSQL + EF Core |
| **Audio Storage** | Azure Blob Storage |
| **Authentication** | Azure Entra ID (admin) |
| **Deployment** | GitHub Actions → Azure Static Web Apps |

---

## Verification Plan

### Phase 1: API Development
1. Create Azure Functions project with local development
2. Run `func start` to test API locally
3. Use Postman/curl to test CRUD endpoints

### Phase 2: UI Integration  
1. Run Blazor WASM with `dotnet run`
2. Configure API base URL for local development
3. Test public pages display data correctly
4. Test admin CRUD pages

### Phase 3: Deployment Test
1. Deploy to Azure Static Web Apps staging slot
2. Verify API connects to Azure PostgreSQL
3. Test all endpoints in staging environment

### Manual Testing
- [ ] Create a meeting via admin page
- [ ] Verify it appears on correct public page based on flags
- [ ] Edit meeting and verify changes
- [ ] Delete meeting and verify removal
