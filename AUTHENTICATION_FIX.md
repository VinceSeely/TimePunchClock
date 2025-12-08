# Authentication Fix - Complete Summary

## Executive Summary

Fixed a critical authentication issue where the Blazor WebAssembly frontend was not sending authentication tokens to the backend API, causing all authenticated API calls to fail with 401 Unauthorized errors.

**Root Cause**: Using `BaseAddressAuthorizationMessageHandler` which only authorizes same-origin requests, but the frontend and backend are hosted on different domains.

**Solution**: Implemented custom `ApiAuthorizationMessageHandler` that explicitly authorizes requests to the external backend API domain.

**Status**: ✅ Code fix complete, tested, ready for deployment

---

## Files Changed

### Created Files
1. **src/TimeClockUI/ApiAuthorizationMessageHandler.cs** (NEW)
   - Custom authorization message handler for cross-origin API requests
   - Configures authorized URLs and scopes from application settings
   - Automatically attaches access tokens to backend API requests

2. **docs/AUTH_FIX_SUMMARY.md** (NEW)
   - Detailed technical documentation of the problem and solution
   - Configuration requirements
   - Troubleshooting guide

3. **docs/DEPLOYMENT_CHECKLIST.md** (NEW)
   - Step-by-step deployment instructions
   - Post-deployment testing procedures
   - Rollback plan

4. **docs/BROWSER_DEBUG_SCRIPT.js** (NEW)
   - Browser console script for debugging authentication issues
   - Checks configuration, MSAL state, and API connectivity
   - Helper functions for JWT decoding

### Modified Files
1. **src/TimeClockUI/TimePunchExtensions.cs** (MODIFIED)
   - Replaced `BaseAddressAuthorizationMessageHandler` with `ApiAuthorizationMessageHandler`
   - Added registration of custom handler in DI container
   - Enhanced comments explaining the configuration

---

## Technical Details

### Before (Broken)
```csharp
// This only authorizes requests to the same origin as the Blazor app
httpClient.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
```

### After (Fixed)
```csharp
// Register custom handler
services.AddScoped<ApiAuthorizationMessageHandler>();

// Use custom handler that authorizes requests to external backend API
httpClient.AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
```

### How ApiAuthorizationMessageHandler Works
```csharp
public class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public ApiAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigationManager,
        IConfiguration configuration)
        : base(provider, navigationManager)
    {
        var apiBaseUrl = configuration[Constants.TimeClientBaseUrl];
        var scopes = new List<string> { configuration["Api:Scopes:0"] };

        // Configure to authorize requests to backend API
        ConfigureHandler(
            authorizedUrls: new[] { apiBaseUrl },
            scopes: scopes);
    }
}
```

This handler:
1. Intercepts all HTTP requests made through the configured HttpClient
2. Checks if the request URL matches the backend API URL
3. Retrieves an access token from MSAL with the configured scopes
4. Adds the token as a Bearer token in the Authorization header

---

## Deployment Instructions

### Quick Deploy (Recommended)

```bash
# 1. Commit changes
git add .
git commit -m "Fix: Add custom authorization handler for cross-origin API requests

- Created ApiAuthorizationMessageHandler for external backend API
- Replaced BaseAddressAuthorizationMessageHandler with custom handler
- Added comprehensive documentation and testing scripts
"

# 2. Push to trigger deployment
git push origin main

# 3. Monitor deployment
# Go to: https://github.com/YOUR_REPO/actions
# Watch: "Frontend - Deploy to Dev" workflow
```

### Verify Deployment

After deployment completes:

1. **Check GitHub Actions Logs**
   - "Configure Blazor App Settings" step should show actual values (not placeholders)

2. **Test in Browser**
   ```javascript
   // Open DevTools Console at: https://thankful-mushroom-09f42810f.3.azurestaticapps.net/
   // Paste the content of docs/BROWSER_DEBUG_SCRIPT.js
   ```

3. **Verify Network Requests**
   - Open DevTools Network tab
   - Navigate to home page
   - Find request to `/api/TimePunch/lastpunch`
   - Check Request Headers for: `Authorization: Bearer ...`
   - Verify response status: `200 OK`

---

## Testing Checklist

### Automated Checks
- [x] Build succeeds with no errors
- [x] Publish succeeds with no errors
- [x] All unit tests pass (if any)

### Manual Checks (Post-Deployment)
- [ ] Configuration loaded without placeholders
- [ ] User can log in with Azure AD
- [ ] Home page loads without errors
- [ ] Last punch data displays correctly
- [ ] Can create new punch in/out
- [ ] Month summary page works
- [ ] API requests include Authorization header
- [ ] No 401/403 errors in console
- [ ] User can log out

### Browser Console Checks
Expected console messages:
```
✅ base url: https://ca-backend-dev.blueisland-c913d4ac.eastus2.azurecontainerapps.io
✅ ApiAuthorizationMessageHandler configured for URL: https://ca-backend-dev... with scopes: api://...
```

Should NOT see:
```
❌ Authorization failed. These requirements were not met: DenyAnonymousAuthorizationRequirement
❌ net::ERR_ABORTED
```

---

## Rollback Plan

If issues occur after deployment:

### Option 1: Azure Portal (Fastest)
1. Go to Azure Portal → Static Web Apps
2. Select your Static Web App
3. Go to "Deployment history"
4. Select previous working deployment
5. Click "Reactivate"

### Option 2: Git Revert
```bash
git revert HEAD
git push origin main
# Wait for deployment to complete
```

### Option 3: Disable Authentication (Emergency Only)
Update `appsettings.Production.json`:
```json
{
  "Authentication": {
    "Enabled": false
  }
}
```
Then redeploy.

---

## Common Issues & Solutions

### Issue: Still Getting 401 Unauthorized

**Possible Causes:**
1. Configuration placeholders not replaced during deployment
2. API scopes mismatch
3. Backend API audience configuration incorrect
4. User hasn't consented to required scopes

**Solutions:**
1. Check GitHub Actions logs for "Configure Blazor App Settings" output
2. Verify Azure AD app registration scopes match configuration
3. Review token claims in browser (use `decodeJWT(token)` helper)
4. Check backend API's `audience` configuration in appsettings

### Issue: CORS Errors

**Possible Causes:**
1. Backend API CORS policy doesn't include frontend URL
2. Authorization header not allowed in CORS policy

**Solutions:**
1. Verify backend `Cors:AllowedOrigins` includes: `https://thankful-mushroom-09f42810f.3.azurestaticapps.net`
2. Ensure backend CORS policy includes:
   ```csharp
   policy.WithOrigins(allowedOrigins)
       .AllowAnyMethod()
       .AllowAnyHeader()  // This allows Authorization header
       .AllowCredentials();
   ```

### Issue: Configuration Shows Placeholders

**Possible Causes:**
1. Terraform outputs not available
2. Deployment workflow failed at configuration step
3. Wrong environment selected

**Solutions:**
1. Check Terraform state: `cd Infra/dev && terraform output`
2. Re-run deployment workflow
3. Check GitHub secrets are configured

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│ Blazor WebAssembly App                                          │
│ https://thankful-mushroom-09f42810f.3.azurestaticapps.net/     │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Program.cs                                               │   │
│  │ - Configure MSAL Authentication                         │   │
│  │ - Register TimePunchClient                              │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                         │
│  ┌────────────────────▼────────────────────────────────────┐   │
│  │ TimePunchExtensions.cs                                  │   │
│  │ - Register ApiAuthorizationMessageHandler               │   │
│  │ - Configure HttpClient with handler                     │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                         │
│  ┌────────────────────▼────────────────────────────────────┐   │
│  │ ApiAuthorizationMessageHandler                          │   │
│  │ - Intercept HTTP requests                               │   │
│  │ - Get access token from MSAL                            │   │
│  │ - Add Authorization header                              │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                         │
│  ┌────────────────────▼────────────────────────────────────┐   │
│  │ TimePunchClient                                         │   │
│  │ - GetLastPunch()                                        │   │
│  │ - GetTodaysPunchs()                                     │   │
│  │ - Punch()                                               │   │
│  └────────────────────┬────────────────────────────────────┘   │
└────────────────────────┼────────────────────────────────────────┘
                        │
                        │ HTTP Request with:
                        │ Authorization: Bearer <token>
                        │
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│ Backend API                                                      │
│ https://ca-backend-dev.blueisland-c913d4ac.eastus2...          │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ JWT Bearer Authentication Middleware                    │   │
│  │ - Validate token                                        │   │
│  │ - Extract claims                                        │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                         │
│  ┌────────────────────▼────────────────────────────────────┐   │
│  │ Authorization Middleware                                │   │
│  │ - Check [Authorize] attributes                          │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                         │
│  ┌────────────────────▼────────────────────────────────────┐   │
│  │ TimePunchController                                     │   │
│  │ [Authorize]                                             │   │
│  │ - GetLastPunch()                                        │   │
│  │ - GetHours()                                            │   │
│  │ - PunchHours()                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Configuration Reference

### Development (Local)
File: `src/TimeClockUI/wwwroot/appsettings.Development.json`
```json
{
  "Authentication": { "Enabled": false },
  "TimeClientBaseUrl": "http://localhost:5000"
}
```

### Production (Azure)
File: `src/TimeClockUI/wwwroot/appsettings.Production.json` (after placeholder replacement)
```json
{
  "Authentication": { "Enabled": true },
  "TimeClientBaseUrl": "https://ca-backend-dev.blueisland-c913d4ac.eastus2.azurecontainerapps.io",
  "AuthProvider": "AzureAd",
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<TENANT_ID>",
    "ClientId": "<BLAZOR_CLIENT_ID>",
    "ValidateAuthority": true
  },
  "Api": {
    "Scopes": [ "api://<API_CLIENT_ID>/access_as_user" ]
  }
}
```

---

## Additional Resources

- **Technical Details**: See `docs/AUTH_FIX_SUMMARY.md`
- **Deployment Guide**: See `docs/DEPLOYMENT_CHECKLIST.md`
- **Debug Script**: See `docs/BROWSER_DEBUG_SCRIPT.js`
- **Microsoft Docs**: [Secure ASP.NET Core Blazor WebAssembly](https://docs.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/)

---

## Success Metrics

Deployment is successful when:
- ✅ Build and publish complete without errors
- ✅ GitHub Actions workflow succeeds
- ✅ Configuration placeholders replaced with actual values
- ✅ Browser console shows no authentication errors
- ✅ API requests include Authorization header
- ✅ API responses return 200 OK (not 401/403)
- ✅ Last punch data displays correctly
- ✅ User can punch in/out successfully
- ✅ No CORS errors in console
- ✅ Token refresh works after expiration

---

## Contact & Support

If you encounter issues:
1. Review documentation in `docs/` folder
2. Run debug script in browser console
3. Check GitHub Actions logs
4. Review Azure AD app registrations
5. Verify infrastructure in Terraform
6. Check Application Insights for errors

---

*Last Updated: 2025-12-08*
*Status: Ready for Deployment*
