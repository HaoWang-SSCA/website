# Implementation Plan - Embedded Video Players

The goal is to allow users to watch videos directly on the public pages (`SundayMessages.razor`, `GospelMeetings.razor`, and `SpecialMeetings.razor`) instead of being redirected to YouTube.

## Proposed Changes

### [SSCA.website.UI](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI)

#### [MODIFY] [app.css](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/wwwroot/css/app.css)
- Add styles for a responsive video container (iframe wrapper).
- Add styles for a "Video Player" section within the meeting card or a modal/overlay for a premium feel.
- *Decision*: We will implement an "expandable" video section within each card for a seamless in-place experience.

#### [MODIFY] [SundayMessages.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SundayMessages.razor)
- Replace the `<a>` tag for videos with a button to toggle video visibility.
- Implement a YouTube URL to Embed URL helper logic.
- Add an `iframe` with the YouTube embed URL.

#### [MODIFY] [GospelMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/GospelMeetings.razor)
- Same as `SundayMessages.razor`.

#### [MODIFY] [SpecialMeetings.razor](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/Pages/SpecialMeetings.razor)
- Same as `SundayMessages.razor`.

## Technical Details: YouTube Embedding
To embed YouTube videos, we need to convert links like `https://www.youtube.com/watch?v=VIDEO_ID` to `https://www.youtube.com/embed/VIDEO_ID`.

### Logical Helper (in @code or a shared utility)
```csharp
private string GetEmbedUrl(string videoUrl)
{
    if (string.IsNullOrEmpty(videoUrl)) return "";
    // Basic YouTube ID extraction (handles watch?v= and youtu.be/)
    var videoId = "";
    if (videoUrl.Contains("watch?v="))
        videoId = videoUrl.Split("watch?v=")[1].Split("&")[0];
    else if (videoUrl.Contains("youtu.be/"))
        videoId = videoUrl.Split("youtu.be/")[1].Split("?")[0];
    
    return $"https://www.youtube.com/embed/{videoId}";
}
```

## Verification Plan

### Manual Verification
- Navigate to each of the three public pages.
- Click the "Video" button on a meeting.
- Verify the video player appears and plays without leaving the site.
- Verify the layout remains responsive.
