# Contact Form Feature - Post Message

## Overview

This feature allows website visitors to send messages directly through the contact page. Messages are submitted via a web form and processed by the backend API, which sends email notifications to the church office via Microsoft 365 SMTP.

## Architecture

```
┌─────────────────────┐     POST /api/contact     ┌─────────────────────┐
│                     │ ───────────────────────▶  │                     │
│   ContactSection    │                           │   ContactFunction   │
│   (Blazor WASM)     │ ◀──────────────────────── │   (Azure Function)  │
│                     │     JSON Response          │                     │
└─────────────────────┘                           └──────────┬──────────┘
                                                             │
                                                             ▼
                                                  ┌─────────────────────┐
                                                  │    EmailService     │
                                                  │  (MailKit / SMTP)   │
                                                  └──────────┬──────────┘
                                                             │
                                                             ▼
                                                  ┌─────────────────────┐
                                                  │   Microsoft 365     │
                                                  │ (smtp.office365.com)│
                                                  └─────────────────────┘
```

## Components

### Frontend (UI)

**File:** `src/SSCA.website.UI/Components/ContactSection.razor`

- Contact form with Name, Email, Phone, and Message fields
- Client-side validation using Data Annotations
- Submits to `/api/contact` endpoint
- Displays success/error messages
- Bilingual UI (Chinese/English)

**File:** `src/SSCA.website.UI/Pages/Contact.razor`

- Dedicated contact page at `/contact` route
- Includes ContactSection component

### Backend (API)

**File:** `src/SSCA.website.API/Functions/ContactFunction.cs`

- HTTP POST endpoint: `/api/contact`
- Anonymous access (no authentication required)
- Validates request fields
- Returns `ContactMessageResponse` with success/error status

**File:** `src/SSCA.website.API/Services/EmailService.cs`

- `IEmailService` interface
- `EmailService` implementation using **MailKit** for SMTP
- Sends formatted HTML and plain text emails via Microsoft 365 SMTP
- Falls back to logging if SMTP not configured

### Shared Models

**File:** `src/SSCA.website.Shared/Models/ContactMessageDto.cs`

```csharp
public class ContactMessageRequest
{
    [Required] public string Name { get; set; }
    [Required, EmailAddress] public string Email { get; set; }
    public string? Phone { get; set; }
    [Required, MinLength(10)] public string Message { get; set; }
}

public class ContactMessageResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
```

## API Endpoint

### POST /api/contact

**Request Body:**
```json
{
    "name": "张三",
    "email": "zhangsan@example.com",
    "phone": "604-123-4567",
    "message": "我想了解更多关于教会的信息..."
}
```

**Success Response (200 OK):**
```json
{
    "success": true,
    "message": "感谢您的留言！我们会尽快回复您。/ Thank you for your message!"
}
```

**Error Response (400 Bad Request):**
```json
{
    "success": false,
    "message": "请输入姓名 / Name is required"
}
```

**Error Response (500 Internal Server Error):**
```json
{
    "success": false,
    "message": "服务器错误，请稍后重试。/ Server error, please try again later."
}
```

## Validation Rules

| Field   | Required | Validation                          |
|---------|----------|-------------------------------------|
| Name    | Yes      | Non-empty string                    |
| Email   | Yes      | Valid email format                  |
| Phone   | No       | Optional                            |
| Message | Yes      | Minimum 10 characters               |

## CSS Styles

Contact form styles are defined in `src/SSCA.website.UI/wwwroot/css/app.css`:

- `.contact-section` - Section container with gradient background
- `.contact-layout` - Two-column grid layout (info + form)
- `.contact-card` - Info cards for email, phone, address
- `.contact-form-card` - Form container
- `.form-success` - Success message display
- `.form-error` - Error message display

## Configuration

### Required Settings (Azure Static Web App → Configuration)

Add these settings in **Azure Portal** → **Static Web App** → **Configuration**:

| Setting Name | Value | Description |
|--------------|-------|-------------|
| `Smtp__Host` | `smtp.office365.com` | Microsoft 365 SMTP server |
| `Smtp__Port` | `587` | SMTP port with TLS |
| `Smtp__Username` | `hao.wang@team.ssca-bc.org` | M365 account email |
| `Smtp__Password` | `[Password/App Password]` | M365 password (see below) |
| `Smtp__FromEmail` | `hao.wang@team.ssca-bc.org` | Sender email address |
| `Smtp__FromName` | `SSCA-BC Website` | Sender display name |
| `ContactEmail` | `tech@ssca-bc.org` | Recipient email |

### Local Development Configuration

Copy `local.settings.template.json` to `local.settings.json` and update:

```json
{
  "Values": {
    "Smtp__Host": "smtp.office365.com",
    "Smtp__Port": "587",
    "Smtp__Username": "hao.wang@team.ssca-bc.org",
    "Smtp__Password": "YOUR_M365_PASSWORD",
    "Smtp__FromEmail": "hao.wang@team.ssca-bc.org",
    "Smtp__FromName": "SSCA-BC Website",
    "ContactEmail": "tech@ssca-bc.org"
  }
}
```

## Microsoft 365 Setup

### 1. Enable Authenticated SMTP
Microsoft 365 often disables SMTP authentication by default for security. It must be enabled for the specific mailbox:
1. Go to [Microsoft 365 Admin Center](https://admin.microsoft.com)
2. **Users** -> **Active users** -> Select the user (`hao.wang@team.ssca-bc.org`)
3. Click the **Mail** tab
4. Click **Manage email apps**
5. Ensure **Authenticated SMTP** is checked
6. Click **Save changes**

### 2. Password Configuration
- **If MFA is NOT enabled:** You can use your regular M365 password.
- **If MFA IS enabled:** You must create an **App Password**:
  1. Go to [mysignins.microsoft.com/security-info](https://mysignins.microsoft.com/security-info)
  2. Click **+ Add sign-in method**
  3. Select **App password**
  4. Name it "SSCA Website"
  5. Copy the 16-character password and use it as `Smtp__Password`.

*Note: If "App password" is not in the list, it means your Organization Admin has disabled it or MFA is not required.*

## Email Format

Emails are sent with both HTML and plain text versions:

### HTML Email Features:
- Branded header with gradient
- Formatted fields (Name, Email, Phone, Message)
- Clickable email link
- Professional footer
- Mobile-responsive design

### Email Subject:
```
网站留言 - 来自 [Name]
```

### Reply-To:
The sender's email is set as Reply-To, so you can respond directly.

## Testing

### Local Development

1. Start the API:
   ```bash
   cd src/SSCA.website.API
   func start
   ```

2. Start the UI:
   ```bash
   cd src/SSCA.website.UI
   dotnet run
   ```

3. Navigate to `http://localhost:5227/contact`

4. Submit test message - check your inbox!

### Fallback Behavior

If SMTP is not configured, the service will:
- Log a warning: "SMTP not configured. Logging message instead of sending email."
- Log the full message content to Application Insights
- Return success to the user (message is recorded)

## Troubleshooting

### Common Issues

1. **"Authentication failed"**
   - Verify **Authenticated SMTP** is enabled in Admin Center (Step 1 above).
   - Check if you need an App Password (Step 2 above).
   - Verify username is the full email address.

2. **"Connection refused"**
   - Check `smtp.office365.com` and port `587`.
   - Verify firewall allows outbound port 587.

3. **Emails not received**
   - Check spam folder.
   - Verify `ContactEmail` setting.
   - Check Application Insights logs for errors.

### Application Insights Query

```kusto
traces
| where message contains "Contact email"
| order by timestamp desc
```

## Security Considerations

- ✅ Sensitive credentials stored in Azure Configuration (secure)
- ✅ HTML content is escaped to prevent XSS
- ✅ Input validated on both client and server
- ⚠️ Consider adding rate limiting to prevent spam
- ⚠️ Consider adding CAPTCHA for bot protection

## Dependencies

- **MailKit** - Modern .NET email library for SMTP
- **MimeKit** - Email message creation (included with MailKit)
