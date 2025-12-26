# Audio Replay Implementation Plan

Audio playback is broken due to two issues:
1. **Backend**: `AzureStorage:BaseUrl` not configured → audio URLs are malformed
2. **Frontend**: Audio shown as external links → users can't play inline

---

## Part 1: Backend - Fix Audio URL Configuration

### Root Cause

In [MeetingService.cs](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.API/Services/MeetingService.cs#L168-L170):
```csharp
AudioUrl = string.IsNullOrEmpty(meeting.AudioBlobName) 
    ? null 
    : $"{_storageBaseUrl}/audio-files/{meeting.AudioBlobName}"
```

The `_storageBaseUrl` is empty because `AzureStorage:BaseUrl` is missing from Static Web App configuration.

### Proposed Fix

#### [MODIFY] [resources.tf](file:///c:/Projects/HaoWang-SSCA/website/infrastructure/resources.tf#L25-L30)

Add the missing `AzureStorage__BaseUrl` app setting:

```diff
 app_settings = {
   "ConnectionStrings__PostgreSQL"         = "Host=${azurerm_postgresql_flexible_server.main.fqdn};..."
   "ConnectionStrings__AzureStorage"       = azurerm_storage_account.audio.primary_connection_string
+  "AzureStorage__BaseUrl"                 = azurerm_storage_account.audio.primary_blob_endpoint
   "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.main.instrumentation_key
   "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
 }
```

> [!IMPORTANT]
> This requires a Terraform apply and redeployment.

---

## Part 2: Frontend - Add Embedded Audio Players

### Public Pages

#### [MODIFY] [SundayMessages.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SundayMessages.razor#L63-L69)

Replace link with audio player:

```diff
 @if (!string.IsNullOrEmpty(meeting.AudioUrl))
 {
-    <a href="@meeting.AudioUrl" target="_blank" class="btn btn-sm btn-outline">
-        <span class="material-symbols-outlined">headphones</span>
-        音频
-    </a>
+    <div class="audio-container">
+        <audio controls preload="none" class="audio-player">
+            <source src="@meeting.AudioUrl" type="audio/mpeg">
+        </audio>
+    </div>
 }
```

Same change applies to:
- [GospelMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/GospelMeetings.razor#L64-L69)
- [SpecialMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SpecialMeetings.razor#L64-L69)

---

### Admin Page

#### [MODIFY] [MeetingEdit.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/Admin/MeetingEdit.razor#L74-L79)

```diff
 @if (!IsNew && existingMeeting != null && !string.IsNullOrEmpty(existingMeeting.AudioUrl))
 {
     <div class="current-audio">
         <span class="material-symbols-outlined">headphones</span>
-        <a href="@existingMeeting.AudioUrl" target="_blank">当前音频 Current Audio</a>
+        <span>当前音频 Current Audio</span>
+        <audio controls preload="none" class="audio-player">
+            <source src="@existingMeeting.AudioUrl" type="audio/mpeg">
+        </audio>
     </div>
 }
```

---

### CSS Styles

#### [MODIFY] CSS file

```css
/* Audio Player */
.audio-container {
    margin-top: 0.5rem;
}

.audio-player {
    width: 100%;
    max-width: 300px;
    height: 36px;
    border-radius: 8px;
}

.current-audio .audio-player {
    margin-top: 0.5rem;
}
```

---

## Verification Plan

1. **Backend**: After Terraform apply, verify API returns correct audio URLs
2. **Frontend**: Navigate to meeting pages and confirm audio plays inline
