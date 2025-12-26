# Implementation Plan - Fix Blocked Video Content

The in-place video player is blocked by the Content Security Policy (CSP) defined in the Azure Static Web App configuration.

## Proposed Changes

### [SSCA.website.UI](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI)

#### [MODIFY] [staticwebapp.config.json](file:///c:/Projects/HaoWang-SSCA/website/src/SSCA.website.UI/wwwroot/staticwebapp.config.json)
- Update `Content-Security-Policy` in `globalHeaders` to allow `frame-src` from `https://www.youtube.com`.
- Specifically, change:
  ```json
  "Content-Security-Policy": "default-src 'self'; style-src 'self' 'unsafe-inline' fonts.googleapis.com; font-src 'self' fonts.gstatic.com; img-src 'self' data: blob:; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; media-src 'self' https://*.blob.core.windows.net;"
  ```
  to:
  ```json
  "Content-Security-Policy": "default-src 'self'; style-src 'self' 'unsafe-inline' fonts.googleapis.com; font-src 'self' fonts.gstatic.com; img-src 'self' data: blob:; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; media-src 'self' https://*.blob.core.windows.net; frame-src https://www.youtube.com https://www.youtube-nocookie.com;"
  ```

## Verification Plan

### Manual Verification
- Deploy the changes (or test locally if the local server respects this config).
- Verify that the video player now loads and plays content correctly.
