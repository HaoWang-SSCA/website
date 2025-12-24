# Message Meetings - Admin CRUD Pages TODO

## Overview
Complete the admin interface for managing Message Meetings (讲道信息).

## Admin Pages to Create

### 1. MeetingList.razor (Admin/MeetingList.razor)
- [ ] Table with all meetings (paginated)
- [ ] Columns: Date, Speaker, Topic, Type (Gospel/Special/Regular), Actions
- [ ] Search/Filter bar
- [ ] "Add New" button
- [ ] Edit/Delete action buttons per row
- [ ] Confirmation dialog for delete

### 2. MeetingEdit.razor (Admin/MeetingEdit.razor)
- [ ] Form for create/edit meeting
- [ ] Fields:
  - Date picker (信息日期)
  - Speaker input (讲员)
  - Topic input (信息主题)
  - Video URL input (YouTube链接)
  - Audio file upload (音频文件)
  - IsGospel checkbox (福音信息)
  - IsSpecialMeeting checkbox (特别聚会)
- [ ] Save/Cancel buttons
- [ ] Validation messages
- [ ] Success/Error toast notifications

### 3. Supporting Components
- [ ] ConfirmDialog.razor - Reusable confirmation modal
- [ ] Toast/Notification component for success/error messages

## API Integration
- [x] GET /api/admin/meetings - List all (already implemented)
- [x] POST /api/admin/meetings - Create (already implemented)
- [x] PUT /api/admin/meetings/{id} - Update (already implemented)
- [x] DELETE /api/admin/meetings/{id} - Delete (already implemented)
- [x] POST /api/admin/meetings/{id}/audio - Upload audio (already implemented)

## Authentication
- [ ] Protect admin routes with Azure Entra ID
- [ ] Add authorize attribute to admin pages
- [ ] Configure authentication in staticwebapp.config.json

---

## Deployment TODO

### Azure Static Web Apps
- [ ] Create staticwebapp.config.json with:
  - Routes configuration
  - API integration settings
  - Authentication settings
- [ ] GitHub Actions workflow for CI/CD

### Azure PostgreSQL
- [ ] Create Azure Database for PostgreSQL Flexible Server
- [ ] Configure firewall rules
- [ ] Run EF Core migrations

### Azure Storage
- [ ] Create Storage Account for audio files
- [ ] Create "audio-files" container
- [ ] Configure CORS for blob access

### Azure Functions
- [ ] Configure app settings in Azure
- [ ] Set connection strings
