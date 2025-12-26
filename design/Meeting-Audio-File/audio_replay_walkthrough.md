# Audio Replay Implementation - Walkthrough

## Summary
Fixed audio playback by adding missing backend configuration and replacing external links with embedded HTML5 audio players.

---

## Changes Made

### Backend: Terraform Configuration

Added missing `AzureStorage__BaseUrl` to Static Web App settings:

render_diffs(file:///c:/Projects/HaoWang-SSCA/website/infrastructure/resources.tf)

> **Next Step**: Run `terraform apply` and redeploy to apply this change.

---

### Frontend: Embedded Audio Players

Replaced external `<a>` links with `<audio>` elements in 4 files:

| File | Change |
|------|--------|
| [SundayMessages.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SundayMessages.razor#L63-L70) | Added audio player |
| [GospelMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/GospelMeetings.razor#L64-L71) | Added audio player |
| [SpecialMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SpecialMeetings.razor#L64-L71) | Added audio player |
| [MeetingEdit.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/Admin/MeetingEdit.razor#L74-L84) | Added preview player |

---

### CSS Styles

Added responsive audio player styling in [app.css](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/wwwroot/css/app.css#L1317-L1330).

---

## Build Verification

- ✅ UI Build: Succeeded
- ✅ API Build: Succeeded

---

## Deployment Required

To complete the fix:
1. Run `terraform apply` in `infrastructure/` folder
2. Push changes to trigger CI/CD deployment
3. Verify audio plays on deployed site
