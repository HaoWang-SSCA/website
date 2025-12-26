# Implementation Plan - Fix YouTube Error 153

"Error 153" is typically caused by YouTube's requirement for a valid referrer. If the browser sends `no-referrer`, the player fails to configure correctly.

## Proposed Changes

### [SSCA.website.UI](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI)

#### [MODIFY] [index.html](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/wwwroot/index.html)
- Add a meta tag for referrer policy: `<meta name="referrer" content="strict-origin-when-cross-origin" />`. This ensures YouTube receives the necessary context.

#### [MODIFY] [SundayMessages.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SundayMessages.razor)
- Add `referrerpolicy="strict-origin-when-cross-origin"` to the `iframe` tag.
- Add `&origin=` parameter to the embed URL (using the current base URL) for extra reliability.

#### [MODIFY] [GospelMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/GospelMeetings.razor)
- Same `iframe` attribute update.

#### [MODIFY] [SpecialMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SpecialMeetings.razor)
- Same `iframe` attribute update.

## Verification Plan

### Manual Verification
- Deploy and verify that Error 153 is resolved.
- Check both standard and `/live/` links.
