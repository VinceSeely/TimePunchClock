# Deployment Checklist - Authentication Fix

## Pre-Deployment Verification

### 1. Code Changes
- [x] Created `ApiAuthorizationMessageHandler.cs`
- [x] Updated `TimePunchExtensions.cs` to use custom handler
- [x] Build succeeded with no errors
- [x] Publish test completed successfully

### 2. Configuration Files
Verify the following files are correct:

**src/TimeClockUI/wwwroot/appsettings.Production.json**
- Contains placeholders that will be replaced during deployment
- `BACKEND_URL_PLACEHOLDER`
- `TENANT_ID_PLACEHOLDER`
- `BLAZOR_CLIENT_ID_PLACEHOLDER`
- `API_CLIENT_ID_PLACEHOLDER`

## Deployment Steps

### Option 1: Manual Deployment via GitHub Actions

1. Push changes to the repository:
   ```bash
   git add .
   git commit -m "Fix: Add custom authorization handler for cross-origin API requests"
   git push origin main
   ```

2. Monitor GitHub Actions workflow:
   - Go to: https://github.com/YOUR_REPO/actions
   - Watch "Frontend - Deploy to Dev" workflow
   - Verify "Configure Blazor App Settings" step replaces placeholders

### Option 2: Manual Trigger

1. Go to GitHub Actions
2. Select "Frontend - Deploy to Dev" workflow
3. Click "Run workflow"
4. Select branch: `main`
5. Click "Run workflow" button

## Post-Deployment Testing

### 1. Verify Configuration Replacement

Check GitHub Actions logs for "Configure Blazor App Settings" step output:
```
Configuration updated:
{
  "Authentication": {
    "Enabled": true
  },
  "TimeClientBaseUrl": "https://ca-backend-dev.blueisland-c913d4ac.eastus2.azurecontainerapps.io",
  "AuthProvider": "AzureAd",
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/YOUR_TENANT_ID",
    "ClientId": "YOUR_BLAZOR_CLIENT_ID",
    "ValidateAuthority": true
  },
  "Api": {
    "Scopes": [ "api://YOUR_API_CLIENT_ID/access_as_user" ]
  }
}
```

### 2. Browser DevTools Testing

#### Console Logs
Open browser console and look for:
- ✅ "ApiAuthorizationMessageHandler configured for URL: https://ca-backend-dev.blueisland-c913d4ac.eastus2.azurecontainerapps.io with scopes: api://..."
- ✅ "base url: https://ca-backend-dev.blueisland-c913d4ac.eastus2.azurecontainerapps.io"
- ❌ NO "Authorization failed" errors
- ❌ NO "ERR_ABORTED" errors

#### Network Tab
1. Open DevTools Network tab
2. Navigate to home page (should trigger API call)
3. Find request to `/api/TimePunch/lastpunch`
4. Check Request Headers:
   ```
   Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6...
   ```
5. Check Response:
   - Status: `200 OK` (not 401 or 403)
   - Contains punch data

#### Application Tab (Token Inspection)
1. Go to Application tab > Storage
2. Expand "IndexedDB" or "Session Storage"
3. Find MSAL cached tokens
4. Decode token at https://jwt.ms (optional)
5. Verify claims include:
   - `aud`: Should match backend API audience
   - `scp`: Should include "access_as_user" or configured scope

### 3. Functional Testing

Test the following features:
- [ ] Login succeeds
- [ ] Home page loads without errors
- [ ] Last punch displays correctly
- [ ] Can create new punch in/out
- [ ] Month summary page loads
- [ ] Logout works correctly

### 4. Error Scenarios

Test error handling:
- [ ] Logout and try to access protected page → Should redirect to login
- [ ] Token expiration → Should auto-refresh or prompt re-login
- [ ] Network offline → Should show appropriate error

## Rollback Plan

If deployment fails or issues occur:

### Quick Rollback
1. Go to Azure Portal
2. Navigate to Static Web App
3. Go to "Deployment history"
4. Select previous working deployment
5. Click "Reactivate"

### Code Rollback
```bash
git revert HEAD
git push origin main
```

## Troubleshooting

### Issue: Configuration values still showing placeholders

**Check:**
1. Terraform outputs are correct: `cd Infra/dev && terraform output`
2. GitHub Actions has correct secrets configured
3. sed commands in workflow executed successfully

**Fix:**
Re-run deployment workflow with verbose logging

### Issue: 401 Unauthorized still occurring

**Check:**
1. Browser console for handler configuration message
2. Network tab for Authorization header presence
3. Token claims match backend API requirements

**Debug:**
```javascript
// Run in browser console to check configuration
await fetch('/appsettings.Production.json').then(r => r.json()).then(console.log)
```

### Issue: CORS errors

**Check:**
1. Backend API CORS configuration includes Static Web App URL
2. Backend API is running and accessible
3. Backend API logs for CORS errors

**Backend API check:**
```bash
# From local terminal
curl -H "Origin: https://thankful-mushroom-09f42810f.3.azurestaticapps.net" \
     -H "Access-Control-Request-Method: GET" \
     -H "Access-Control-Request-Headers: authorization" \
     -X OPTIONS \
     https://ca-backend-dev.blueisland-c913d4ac.eastus2.azurecontainerapps.io/api/TimePunch/lastpunch
```

Expected response headers:
```
Access-Control-Allow-Origin: https://thankful-mushroom-09f42810f.3.azurestaticapps.net
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: authorization, content-type
```

## Success Criteria

Deployment is successful when:
- ✅ GitHub Actions workflow completes without errors
- ✅ Health check passes
- ✅ Browser console shows no authentication errors
- ✅ API requests include Authorization header
- ✅ Last punch data loads successfully
- ✅ User can punch in/out
- ✅ No CORS errors
- ✅ Token refresh works (test by waiting > 1 hour)

## Monitoring

After deployment, monitor:
1. Azure Application Insights for exceptions
2. Static Web App logs
3. Container App (backend) logs
4. User feedback

### Check logs:
```bash
# Static Web App logs (via Azure CLI)
az staticwebapp show --name <swa-name> --resource-group <rg-name>

# Container App logs
az containerapp logs show --name <app-name> --resource-group <rg-name> --follow
```

## Next Steps

After successful deployment:
1. Update documentation with actual URLs and configuration
2. Create monitoring alerts for authentication failures
3. Test token refresh scenarios
4. Review Azure AD app registration permissions
5. Consider implementing additional security measures (e.g., certificate-based auth)

## Support

If issues persist:
1. Check documentation: `docs/AUTH_FIX_SUMMARY.md`
2. Review Azure AD app registrations
3. Verify infrastructure setup in Terraform
4. Contact team for assistance
