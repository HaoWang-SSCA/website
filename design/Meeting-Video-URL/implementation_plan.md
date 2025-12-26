# Implementation Plan - Video URL Validation

The goal is to add URL validation to the video link field in the meeting edit form to prevent invalid data from being submitted.

## Proposed Changes

### [SSCA.website.Shared](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.Shared)

#### [MODIFY] [CreateMeetingRequest.cs](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.Shared/Models/CreateMeetingRequest.cs)
- Add `[Url]` validation attribute to `VideoUrl`.

#### [MODIFY] [UpdateMeetingRequest.cs](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.Shared/Models/UpdateMeetingRequest.cs)
- Add `[Url]` validation attribute to `VideoUrl`.

---

### [SSCA.website.UI](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI)

#### [MODIFY] [MeetingEdit.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/Admin/MeetingEdit.razor)
- Add `[Url]` validation attribute to `MeetingFormModel.VideoUrl`.
- Add `<ValidationMessage For="@(() => model.VideoUrl)" />` below the `VideoUrl` input.

## Verification Plan

### Manual Verification
- Run the UI locally.
- Navigate to the meeting edit or create page.
- Enter an invalid URL (e.g., "invalid-url") and click "Save".
- Verify that a validation error message is displayed.
- Enter a valid URL (e.g., "https://www.youtube.com/watch?v=dQw4w9WgXcQ") and verify it saves successfully.
