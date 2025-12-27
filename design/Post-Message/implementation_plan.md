# Contact Form Implementation Plan

## Phase 1: Basic Form (Completed ✅)

### 1.1 Create Contact Page
- [x] Create `ContactSection.razor` component
- [x] Create `Contact.razor` page at `/contact` route
- [x] Add contact info cards (email, phone, address)
- [x] Add embedded Google Map
- [x] Update navigation links from `#contact` to `/contact`

### 1.2 Create Contact Form UI
- [x] Add form fields: Name, Email, Phone, Message
- [x] Client-side validation with error messages
- [x] Loading state during submission
- [x] Success message after submission
- [x] Bilingual labels (Chinese/English)
- [x] Responsive layout (info cards + form side by side)

### 1.3 Add CSS Styles
- [x] Contact section layout styles
- [x] Contact cards with icons
- [x] Contact form card styles
- [x] Form input styles
- [x] Success/error message styles
- [x] Mobile responsive design

## Phase 2: Backend API (Completed ✅)

### 2.1 Create Shared Models
- [x] `ContactMessageRequest` DTO
- [x] `ContactMessageResponse` DTO
- [x] Validation attributes

### 2.2 Create Email Service
- [x] `IEmailService` interface
- [x] `EmailService` implementation
- [x] Logging to Application Insights
- [x] Register in DI container

### 2.3 Create Contact Function
- [x] POST `/api/contact` endpoint
- [x] Request validation
- [x] Error handling
- [x] Bilingual response messages

### 2.4 Update Frontend
- [x] Call API instead of mailto
- [x] Handle API responses
- [x] Display error messages
- [x] Network error handling

## Phase 3: Email Integration (Future)

### 3.1 SendGrid Setup
- [ ] Add SendGrid package
- [ ] Create SendGrid account
- [ ] Configure API key in Azure Static Web App settings
- [ ] Update `EmailService` to send emails

### 3.2 Email Template
- [ ] Design HTML email template
- [ ] Include all form fields
- [ ] Add church branding
- [ ] Test email delivery

## Phase 4: Security Enhancements (Future)

### 4.1 Rate Limiting
- [ ] Implement rate limiting in Azure Functions
- [ ] Limit by IP address
- [ ] Return 429 for exceeded limits

### 4.2 CAPTCHA
- [ ] Integrate Google reCAPTCHA
- [ ] Add CAPTCHA component to form
- [ ] Verify token on backend

### 4.3 Input Sanitization
- [ ] Sanitize HTML in messages
- [ ] Prevent XSS attacks
- [ ] Log suspicious submissions

## Files Changed

### New Files
- `src/SSCA.website.UI/Pages/Contact.razor`
- `src/SSCA.website.UI/Components/ContactSection.razor`
- `src/SSCA.website.Shared/Models/ContactMessageDto.cs`
- `src/SSCA.website.API/Services/EmailService.cs`
- `src/SSCA.website.API/Functions/ContactFunction.cs`
- `design/Post-Message/README.md`
- `design/Post-Message/implementation_plan.md`

### Modified Files
- `src/SSCA.website.UI/wwwroot/css/app.css` - Added contact form styles
- `src/SSCA.website.UI/wwwroot/staticwebapp.config.json` - Added Google Maps CSP
- `src/SSCA.website.UI/Shared/Header.razor` - Updated nav links
- `src/SSCA.website.API/Program.cs` - Registered EmailService

## Testing Checklist

- [x] Form validation works for all required fields
- [x] Error messages display correctly
- [x] Loading spinner shows during submission
- [x] Success message shows after submission
- [x] "Send New Message" resets form
- [x] API endpoint builds successfully
- [ ] E2E test with deployed API
- [ ] Email delivery test (after SendGrid integration)
