# Implementation Plan - Fix YouTube Playback Error

The user reports a "Playback ID" error in the embedded YouTube player. This can be caused by incorrect URL extraction or restrictive headers/CSP.

## Proposed Changes

### [SSCA.website.UI](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI)

#### [MODIFY] [staticwebapp.config.json](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/wwwroot/staticwebapp.config.json)
- Change `X-Frame-Options` to `SAMEORIGIN`.
- Update `Content-Security-Policy`:
  - `frame-src`: use `https://*.youtube.com https://*.youtube-nocookie.com;`
  - `img-src`: add `https://*.ytimg.com` for video thumbnails.
  - `script-src`: add `'unsafe-eval'` (already there, but YouTube sometimes needs it) and ensure no other conflicts.

#### [MODIFY] [SundayMessages.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SundayMessages.razor)
- Improve `GetEmbedUrl` logic to handle various YouTube URL formats (watch?v=, youtu.be/, etc.) more reliably.
- Use `https://www.youtube-nocookie.com/embed/` for the embed URL.

#### [MODIFY] [GospelMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/GospelMeetings.razor)
- Apply same `GetEmbedUrl` improvements.

#### [MODIFY] [SpecialMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SpecialMeetings.razor)
- Apply same `GetEmbedUrl` improvements.

## Verification Plan

### Manual Verification
- Test with standard YouTube URLs and shortened `youtu.be` URLs.
- Verify that the video plays correctly without the "Playback ID" error.
