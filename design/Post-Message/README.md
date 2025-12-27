# Contact Form Feature - Post Message

## Overview

This feature allows website visitors to send messages directly through the contact page. Messages are submitted via a web form and processed by the backend API, which can then send email notifications to the church office.

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
                                                  │  (Logs / SendGrid)  │
                                                  └──────────┬──────────┘
                                                             │
                                                             ▼
                                                  ┌─────────────────────┐
                                                  │  Application        │
                                                  │  Insights / Email   │
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
- `EmailService` implementation
- Currently logs messages to Application Insights
- Ready for SendGrid/Azure Communication Services integration

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

### Email Recipient

Set in API configuration:
```json
{
    "ContactEmail": "tech@ssca-bc.org"
}
```

Default: `tech@ssca-bc.org`

## Future Enhancements

### Email Integration Options

1. **SendGrid** (Recommended)
   - Add package: `SendGrid`
   - Configure API key in app settings
   - Update `EmailService` to send actual emails

2. **Azure Communication Services**
   - Add package: `Azure.Communication.Email`
   - Configure connection string
   - Update `EmailService` implementation

3. **SMTP**
   - Use `System.Net.Mail.SmtpClient`
   - Configure SMTP server settings

### Example SendGrid Implementation

```csharp
public async Task<bool> SendContactMessageAsync(ContactMessageRequest request)
{
    var apiKey = _configuration["SendGrid:ApiKey"];
    var client = new SendGridClient(apiKey);
    
    var from = new EmailAddress("noreply@ssca-bc.org", "SSCA-BC Website");
    var to = new EmailAddress(_configuration["ContactEmail"]);
    var subject = $"网站留言 - 来自 {request.Name}";
    var plainText = $"姓名: {request.Name}\n电邮: {request.Email}\n电话: {request.Phone}\n\n留言:\n{request.Message}";
    
    var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, null);
    var response = await client.SendEmailAsync(msg);
    
    return response.StatusCode == HttpStatusCode.OK 
        || response.StatusCode == HttpStatusCode.Accepted;
}
```

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

4. Submit test message - check Application Insights logs

### Production

Messages are logged to Azure Application Insights. View logs in Azure Portal → Application Insights → Logs.

Query:
```kusto
traces
| where message contains "Contact form submission"
| order by timestamp desc
```

## Security Considerations

- Rate limiting should be implemented to prevent spam
- Consider adding CAPTCHA for bot protection
- Validate and sanitize all input on the server
- Do not expose internal error details to clients
