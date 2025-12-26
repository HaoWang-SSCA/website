# Audio Upload Workflow Implementation

## Goal
Allow audio upload during meeting creation by decoupling the audio upload from the meeting ID.

## Proposed Changes

### API Changes

#### [NEW] `POST /api/mgmt/audio-upload`
- **Purpose**: Upload audio file without meeting ID
- **Request**: `multipart/form-data` with audio file
- **Response**: `{ blobName: "2025/guid-filename.mp3", url: "https://..." }`

#### [MODIFY] `CreateMeetingRequest`
- Add `AudioBlobName` field (optional)

#### [MODIFY] `UpdateMeetingRequest`  
- Add `AudioBlobName` field (optional)

#### [MODIFY] `MeetingService.CreateAsync`
- Save `AudioBlobName` from request

#### [MODIFY] `MeetingService.UpdateAsync`
- Update `AudioBlobName` if provided

---

### Frontend Changes

#### [MODIFY] `MeetingEdit.razor`
1. Always show audio upload section (not just when editing)
2. On file upload:
   - Call `/api/mgmt/audio-upload`
   - Store returned `blobName` in component state
3. On create/update:
   - Include `audioBlobName` in request body

#### [MODIFY] `MeetingFormModel`
- Add `AudioBlobName` field

---

### Files to Modify
1. `AdminMeetingsFunction.cs` - Add new upload endpoint
2. `CreateMeetingRequest.cs` - Add AudioBlobName
3. `UpdateMeetingRequest.cs` - Add AudioBlobName
4. `MeetingService.cs` - Handle AudioBlobName in create/update
5. `MeetingEdit.razor` - Update UI flow

## Verification
- Create meeting with audio upload
- Edit meeting with new audio upload
- Verify blob storage contains uploaded files
