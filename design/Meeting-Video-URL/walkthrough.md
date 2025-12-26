# Walkthrough - Video URL Validation

I have implemented video URL validation for the meeting registration and editing forms. This ensures that only valid URLs can be submitted for the video link field.

## Changes Made

### Shared Models
I added the `[Url]` validation attribute to both `CreateMeetingRequest` and `UpdateMeetingRequest` to ensure the backend receives valid data.

- [CreateMeetingRequest.cs](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.Shared/Models/CreateMeetingRequest.cs#L22)
- [UpdateMeetingRequest.cs](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.Shared/Models/UpdateMeetingRequest.cs#L25)

### Admin UI
I updated the `MeetingEdit.razor` page to include client-side validation for the video URL.

- Added `[Url]` attribute to the internal `MeetingFormModel`.
- Added a `ValidationMessage` component to display errors if an invalid URL is entered.
- Added the missing `System.ComponentModel.DataAnnotations` directive.

- [MeetingEdit.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/Admin/MeetingEdit.razor)

## Verification Results

### Build Status
I verified that the project builds successfully after the changes.

```bash
dotnet build src/SSCA.website.UI/SSCA_BC.csproj
...
Build succeeded in 10.5s
```

### Manual Testing (Recommended)
You can verify this by:
1. Navigating to the meeting create or edit page.
2. Entering some text that is not a URL in the "Video URL" field.
3. Clicking "Save".
4. You should see a validation error message.
5. Enter a valid URL (e.g., `https://youtube.com/watch?v=...`) and it should pass validation.
