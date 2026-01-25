# Speaker Dropdown with Caching Implementation

## Overview

This document describes the implementation of a speaker dropdown feature in the meeting search sections, along with a two-tier caching strategy to optimize performance.

## Feature Description

The speaker search field on all three meeting pages (Sunday Messages, Gospel Meetings, and Special Meetings) has been changed from a free-text input to a dropdown select. This provides:

- **Better user experience**: Users can quickly select from available speakers
- **Data consistency**: Prevents typos and ensures valid speaker names
- **Reduced API load**: Speaker list is cached both on server and client

---

## Architecture

### Two-Tier Caching Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                        Browser (Client)                         │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  SpeakerService (Singleton)                              │   │
│  │  - Caches speakers for entire browser session            │   │
│  │  - Deduplicates concurrent requests                      │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│                    GET /api/meetings/speakers                    │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                        API Server                                │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  MeetingService with IMemoryCache                        │   │
│  │  - Caches speakers for 1 hour                            │   │
│  │  - Invalidates on Create/Update/Delete operations        │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│                    PostgreSQL Database                           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Implementation Details

### 1. Backend (API)

#### New API Endpoint

**GET /api/meetings/speakers**

Returns a list of distinct speaker names from all meetings, sorted alphabetically.

**Location**: `src/SSCA.website.API/Functions/MeetingsFunction.cs`

```csharp
[Function("GetSpeakers")]
public async Task<IActionResult> GetSpeakers(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "meetings/speakers")] HttpRequest req)
{
    var speakers = await _meetingService.GetDistinctSpeakersAsync();
    return new OkObjectResult(speakers);
}
```

#### Service Layer with Caching

**Location**: `src/SSCA.website.API/Services/MeetingService.cs`

```csharp
private const string SpeakersCacheKey = "DistinctSpeakers";
private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

public async Task<List<string>> GetDistinctSpeakersAsync()
{
    if (_cache.TryGetValue(SpeakersCacheKey, out List<string>? cachedSpeakers) && cachedSpeakers != null)
    {
        return cachedSpeakers;
    }

    var speakers = await _context.MessageMeetings
        .Select(m => m.Speaker)
        .Distinct()
        .OrderBy(s => s)
        .ToListAsync();

    _cache.Set(SpeakersCacheKey, speakers, CacheDuration);
    return speakers;
}
```

#### Cache Invalidation

The cache is automatically invalidated when meetings are created, updated, or deleted:

```csharp
private void InvalidateSpeakersCache()
{
    _cache.Remove(SpeakersCacheKey);
}
```

Called in:
- `CreateAsync()` - After creating a new meeting
- `UpdateAsync()` - After updating a meeting
- `DeleteAsync()` - After deleting a meeting

#### Dependencies

**Location**: `src/SSCA.website.API/Program.cs`

```csharp
// Add caching
services.AddMemoryCache();
```

---

### 2. Frontend (Blazor Client)

#### SpeakerService

**Location**: `src/SSCA.website.UI/Services/SpeakerService.cs`

A singleton service that caches the speakers list for the entire browser session:

```csharp
public class SpeakerService
{
    private List<string>? _cachedSpeakers;
    private Task<List<string>>? _loadingTask;

    public async Task<List<string>> GetSpeakersAsync()
    {
        // Return cached data if available
        if (_cachedSpeakers != null)
            return _cachedSpeakers;

        // If already loading, wait for the existing task
        if (_loadingTask != null)
            return await _loadingTask;

        // Start loading
        _loadingTask = LoadSpeakersFromApiAsync();
        _cachedSpeakers = await _loadingTask;
        return _cachedSpeakers;
    }

    public void InvalidateCache()
    {
        _cachedSpeakers = null;
    }
}
```

#### Service Registration

**Location**: `src/SSCA.website.UI/Program.cs`

```csharp
// Register SpeakerService as singleton to cache speakers across the session
builder.Services.AddSingleton<SpeakerService>();
```

#### Usage in Pages

All three meeting pages inject and use the service:

```razor
@using SSCA.website.UI.Services
@inject SpeakerService SpeakerService

<!-- Dropdown in the search section -->
<select id="speaker" @bind="searchSpeaker">
    <option value="">-- 全部讲员 All Speakers --</option>
    @foreach (var speaker in speakers)
    {
        <option value="@speaker">@speaker</option>
    }
</select>

@code {
    private List<string> speakers = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadSpeakers();
        await LoadMeetings();
    }

    private async Task LoadSpeakers()
    {
        speakers = await SpeakerService.GetSpeakersAsync();
    }
}
```

---

## Files Modified

| File | Changes |
|------|---------|
| `src/SSCA.website.API/Program.cs` | Added `services.AddMemoryCache()` |
| `src/SSCA.website.API/Services/MeetingService.cs` | Added caching for speakers, cache invalidation on CRUD |
| `src/SSCA.website.API/Functions/MeetingsFunction.cs` | Added `GetSpeakers` endpoint |
| `src/SSCA.website.UI/Program.cs` | Registered `SpeakerService` as singleton |
| `src/SSCA.website.UI/Services/SpeakerService.cs` | **New file** - Client-side caching service |
| `src/SSCA.website.UI/Pages/SundayMessages.razor` | Changed input to dropdown, use SpeakerService |
| `src/SSCA.website.UI/Pages/GospelMeetings.razor` | Changed input to dropdown, use SpeakerService |
| `src/SSCA.website.UI/Pages/SpecialMeetings.razor` | Changed input to dropdown, use SpeakerService |

---

## Performance Characteristics

### Request Scenarios

| Scenario | Client Cache | Server Cache | Database Query |
|----------|--------------|--------------|----------------|
| First visit to any meeting page | Miss | Miss | Yes |
| Same page reload | Hit | - | No |
| Navigate to different meeting page | Hit | - | No |
| New browser session (within 1 hour) | Miss | Hit | No |
| After CRUD operation | Invalidated | Invalidated | Yes |
| After 1 hour (server) | Valid | Expired | Yes |

### Cache Lifetimes

| Cache Type | Lifetime | Invalidation |
|------------|----------|--------------|
| Client (SpeakerService) | Browser session | Manual call or page refresh |
| Server (IMemoryCache) | 1 hour | On meeting Create/Update/Delete |

---

## Future Improvements

1. **Real-time invalidation**: Use SignalR to notify clients when speakers change
2. **LocalStorage persistence**: Cache speakers in browser storage for faster initial load
3. **Cache warming**: Pre-load speakers cache on application startup
4. **Filter by meeting type**: Optionally filter speakers based on meeting type (Sunday/Gospel/Special)

---

## Testing

### Manual Testing Steps

1. Navigate to any meeting page (e.g., `/sunday-messages`)
2. Verify the speaker dropdown is populated
3. Open browser DevTools > Network tab
4. Navigate to another meeting page (e.g., `/gospel-meetings`)
5. Verify no additional API call to `/api/meetings/speakers`
6. Refresh the page
7. Verify one API call is made to refresh the cache

### API Testing

```bash
# Get speakers list
curl https://your-site.com/api/meetings/speakers

# Expected response
["Speaker A", "Speaker B", "Speaker C"]
```

---

## Notes

- The server-side cache duration (1 hour) can be adjusted in `MeetingService.cs` by modifying the `CacheDuration` constant
- The client-side cache persists until the page is hard-refreshed or the browser tab is closed
- The caching strategy is designed to balance freshness with performance
