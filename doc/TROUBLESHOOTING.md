# Troubleshooting Guide

Common issues encountered during development and deployment, with solutions.

---

## API Function Errors

### Route Parameter Binding Error (GUID)
**Error:** `Cannot convert input parameter 'id' to type 'System.Guid' from type 'System.String'. Error: 's' is an invalid start of a value.`

**Cause:** Route ambiguity. Routes like `meetings/sunday` and `meetings/{id}` conflict because `{id}` matches any string.

**Solution:** Add route constraint:
```csharp
// Before
Route = "meetings/{id}"

// After
Route = "meetings/{id:guid}"
```

---

### EF Core Constant Expression Error
**Error:** `The client projection contains a reference to a constant expression of 'MeetingService' through the instance method 'ToDto'.`

**Cause:** Calling an instance method inside a LINQ `.Select()` that EF Core tries to translate to SQL.

**Solution:** Materialize the query first, then map:
```csharp
// Before (fails)
var items = await query.Select(m => ToDto(m)).ToListAsync();

// After (works)
var entities = await query.ToListAsync();
var items = entities.Select(m => ToDto(m)).ToList();
```

---

### Admin API Returns 404
**Symptoms:** 
- Public APIs (`/api/meetings/sunday`) work fine
- Admin APIs (`/api/admin/meetings`) return 404
- User is authenticated (verified via `/.auth/me`)

**Possible Causes & Solutions:**

1. **AuthorizationLevel.Function** – SWA managed functions don't support function keys
   ```csharp
   // Wrong (returns 404 in SWA)
   [HttpTrigger(AuthorizationLevel.Function, ...)]
   
   // Correct (let staticwebapp.config.json handle auth)
   [HttpTrigger(AuthorizationLevel.Anonymous, ...)]
   ```

2. **NavigationFallback interference** – Add `/api/*` to exclude list:
   ```json
   "navigationFallback": {
       "exclude": ["/api/*", "/css/*", ...]
   }
   ```

---

## Azure AD Authentication Issues

### Login Redirects to Wrong Tenant (`/common/`)
**Cause:** SWA is using the default built-in AAD provider, not your custom config.

**Solutions:**
1. Ensure SWA is on **Standard tier** (Free tier doesn't support custom AAD)
2. Add `AAD_CLIENT_ID` and `AAD_CLIENT_SECRET` to SWA Configuration
3. Verify `openIdIssuer` in `staticwebapp.config.json` has your tenant ID

---

### Login Loop (Credentials Accepted, Then Redirects Back)
**Cause:** Invalid or expired client secret.

**Solution:**
1. Go to App Registration → Certificates & secrets
2. Create a **new** client secret
3. Copy the **Value** (not Secret ID)
4. Update `AAD_CLIENT_SECRET` in SWA Configuration
5. Test in incognito window

---

### 404 on `/.auth/login/aad` Endpoint
**Causes:**
1. **SWA on Free tier** – Custom AAD requires Standard tier (~$9/month)
2. **Missing Application Settings** – Verify `AAD_CLIENT_ID` and `AAD_CLIENT_SECRET` exist
3. **Wrong Redirect URI** – Must be exactly: `https://<your-swa-url>/.auth/login/aad/callback`

---

### ID Tokens Not Enabled
**Symptom:** Login completes but session isn't created.

**Solution:**
1. App Registration → Authentication
2. Enable **ID tokens (used for implicit and hybrid flows)**
3. Save

---

## Content Security Policy (CSP) Errors

### Image Blocked by CSP
**Error:** `Loading the image violates the Content Security Policy directive: "img-src 'self' data: blob:"`

**Solutions:**
1. **Copy images locally** (recommended) – Download external images to `wwwroot/images/`
2. **Update CSP** – Add the external domain:
   ```json
   "Content-Security-Policy": "img-src 'self' data: blob: https://example.com;"
   ```

---

## CI/CD Issues

### Terraform Auth Error in GitHub Actions
**Error:** `The tenant ID was not specified`

**Solution:** Add Azure Login step before Terraform commands:
```yaml
- name: Azure Login
  uses: azure/login@v1
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}
```

---

### GitHub Secret Update Fails (403 Forbidden)
**Error:** `Resource not accessible by personal access token`

**Cause:** Fine-grained PAT missing `Secrets: Read and Write` permission.

**Solution:** Update your `GH_PAT` token permissions in GitHub Settings → Developer settings.

---

## Debugging Tips

### Check Auth Status
Visit `/.auth/me` to see current user info, roles, and claims.

### Check Function Registration
Azure Portal → Static Web App → Functions → See if your function is listed.

### Check Application Insights
Look for startup errors, failed dependency calls, or exceptions.

### Test API With Curl
```bash
# Public endpoint
curl -s -o /dev/null -w "%{http_code}" https://your-swa.azurestaticapps.net/api/meetings/sunday

# Protected endpoint (will return 302 if not authenticated)
curl -s -o /dev/null -w "%{http_code}" https://your-swa.azurestaticapps.net/api/admin/meetings
```
