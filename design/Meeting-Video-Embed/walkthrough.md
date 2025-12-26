# Walkthrough - Embedded Video Players

I have implemented in-place video playback for the public meeting pages. Users can now watch YouTube videos directly within the meeting cards without leaving the website.

## Changes Made

### Styling
I added responsive video player styles to `app.css` to ensure the embedded player fits perfectly within the meeting cards across all device sizes.

- [app.css](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/wwwroot/css/app.css#L1430-1464)

### Public Pages
I updated the following pages to replace the external YouTube links with a toggleable embedded player:

1. **Sunday Messages**: [SundayMessages.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SundayMessages.razor)
2. **Gospel Meetings**: [GospelMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/GospelMeetings.razor)
3. **Special Meetings**: [SpecialMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SpecialMeetings.razor)

Each page now includes:
- A "视频" (Video) button that toggles the player.
- Logic to robustly extract YouTube IDs from various link formats (watch?v=, youtu.be, embed, etc.).
- Use the privacy-focused `youtube-nocookie.com` domain for better compatibility.
- A responsive `iframe` container for the video.

## Verification Results

### Build Status
I verified that the project builds successfully with the new changes.

```bash
dotnet build src/SSCA.website.UI/SSCA_BC.csproj
...
Build succeeded in 14.3s
```

### Manual Testing (Recommended)
1. Go to any public meeting page (e.g., [Sunday Messages](file:///sunday-messages)).
2. Find a meeting with a video URL.
3. Click the "视频" button.
4. The video should expand and play within the card.
5. Click "关闭" (Close) to hide the video player.

## CSP and Header Update
I updated the security settings in `staticwebapp.config.json` to resolve playback blocks:
- Set `X-Frame-Options` to `SAMEORIGIN`.
- Expanded `Content-Security-Policy` `frame-src` to allow all YouTube subdomains.
- Added `https://*.ytimg.com` to `img-src` to allow YouTube thumbnails.

- [staticwebapp.config.json](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/wwwroot/staticwebapp.config.json)
