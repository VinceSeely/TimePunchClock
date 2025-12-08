# Authentication Fix Summary

## Problem

The Blazor WebAssembly application was making API calls to the backend WITHOUT authentication tokens, resulting in:
- HTTP requests to `/api/TimePunch/lastpunch` failing with `net::ERR_ABORTED`
- Console error: "Authorization failed. These requirements were not met: DenyAnonymousAuthorizationRequirement: Requires an authenticated user"
- No Authorization header being sent with backend API requests
- MSAL library loaded but tokens not being attached to HTTP requests

## Root Cause

The application was using `BaseAddressAuthorizationMessageHandler`, which is designed for scenarios where the API and Blazor app share the same base address (same origin). However, in this deployment:

- **Frontend**: `https://thankful-mushroom-09f42810f.3.azurestaticapps.net/`
- **Backend API**: `https://ca-backend-dev.blueisland-c913d4ac.eastus2.azurecontainerapps.io`

The `BaseAddressAuthorizationMessageHandler` only attaches tokens to requests going to the **same origin** as the Blazor app, which is why requests to the backend API were not receiving the Authorization header.

## Solution

Created a custom `ApiAuthorizationMessageHandler` that:

1. Extends `AuthorizationMessageHandler` to intercept HTTP requests
2. Configures authorized URLs to include the backend API base URL
3. Configures API scopes from application settings
4. Attaches access tokens to all requests going to the backend API

### Files Created

**C:\source\vincedevwork\TimePunchClock\src\TimeClockUI\ApiAuthorizationMessageHandler.cs**
- Custom authorization message handler for external backend API
- Reads backend URL and scopes from configuration
- Configures the handler to authorize requests to the backend

### Files Modified

**C:\source\vincedevwork\TimePunchClock\src\TimeClockUI\TimePunchExtensions.cs**
- Replaced `BaseAddressAuthorizationMessageHandler` with `ApiAuthorizationMessageHandler`
- Registers the custom handler in dependency injection
- Attaches the handler to the named HttpClient for API requests

## How It Works

### 1. Service Registration (Program.cs)
```csharp
builder.Services.RegsiterTimeClient(builder.Configuration);
```

### 2. HttpClient Configuration (TimePunchExtensions.cs)
```csharp
// Register the custom authorization message handler for API requests
services.AddScoped<ApiAuthorizationMessageHandler>();

var httpClient = services.AddHttpClient(Constants.TimeClientString, client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

// Use our custom handler that authorizes requests to the external backend API
httpClient.AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
```

### 3. Custom Handler (ApiAuthorizationMessageHandler.cs)
```csharp
ConfigureHandler(
    authorizedUrls: new[] { apiBaseUrl },
    scopes: scopes);
```

This configuration ensures that:
- All HTTP requests made through the named HttpClient (`"timeClient"`) are intercepted
- The handler checks if the request URL matches the configured backend API URL
- If it matches, the handler retrieves an access token from MSAL with the configured scopes
- The token is added to the request as a Bearer token in the Authorization header

## Configuration Requirements

The following settings must be configured in `appsettings.Production.json`:

```json
{
  "Authentication": {
    "Enabled": true
  },
  "TimeClientBaseUrl": "https://your-backend-api-url",
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

## Testing the Fix

After deployment, verify:

1. **Network Tab**: Check that API requests include `Authorization: Bearer <token>` header
2. **Console**: Should see log message: "ApiAuthorizationMessageHandler configured for URL: ..."
3. **API Responses**: Should return 200 OK instead of 401 Unauthorized
4. **Functionality**: Last punch data should load successfully

## Common Issues and Troubleshooting

### Issue: Still getting 401 Unauthorized

**Possible Causes:**
1. API scopes not configured correctly
2. Backend API audience doesn't match the token audience
3. User hasn't consented to the required scopes
4. Token doesn't include the required role/scope claims

**Solution:**
- Verify Azure AD app registration scopes match configuration
- Check backend API's `audience` configuration
- Review token claims in browser developer tools (Application tab > Storage > Access Token)

### Issue: CORS errors

**Possible Causes:**
1. Backend API CORS policy doesn't include the frontend URL
2. Authorization header not allowed in CORS policy

**Solution:**
- Verify backend API `Cors:AllowedOrigins` includes the Static Web App URL
- Ensure `AllowAnyHeader()` is configured in CORS policy

### Issue: Configuration values not applied

**Possible Causes:**
1. Deployment workflow didn't replace placeholders
2. Wrong appsettings file being used (Development vs Production)

**Solution:**
- Check GitHub Actions logs for "Configure Blazor App Settings" step
- Verify placeholders were replaced with actual values
- Ensure .NET environment is set correctly

## Deployment Notes

The GitHub Actions workflow (`frontend-deploy-dev.yml`) automatically:
1. Retrieves infrastructure details from Terraform
2. Replaces placeholders in `appsettings.Production.json` with actual values
3. Builds and publishes the Blazor app
4. Deploys to Azure Static Web Apps

The placeholders are:
- `BACKEND_URL_PLACEHOLDER` → Backend Container App URL
- `TENANT_ID_PLACEHOLDER` → Azure AD Tenant ID
- `BLAZOR_CLIENT_ID_PLACEHOLDER` → Blazor App Registration Client ID
- `API_CLIENT_ID_PLACEHOLDER` → API App Registration Client ID

## Additional Improvements Recommended

1. **Error Handling**: Add more detailed error logging in the custom handler
2. **Token Refresh**: Implement automatic token refresh on 401 responses
3. **Retry Logic**: Add retry logic with exponential backoff for transient failures
4. **Health Check**: Add a dedicated health check endpoint that tests authentication
5. **Monitoring**: Add Application Insights to track authentication failures

## Related Files

- `C:\source\vincedevwork\TimePunchClock\src\TimeClockUI\Program.cs` - Service registration
- `C:\source\vincedevwork\TimePunchClock\src\TimeClockUI\TimePunchExtensions.cs` - HttpClient configuration
- `C:\source\vincedevwork\TimePunchClock\src\TimeClockUI\ApiAuthorizationMessageHandler.cs` - Custom handler
- `C:\source\vincedevwork\TimePunchClock\src\TimeClockUI\wwwroot\appsettings.Production.json` - Production config
- `C:\source\vincedevwork\TimePunchClock\src\TimeApi\Program.cs` - Backend API auth setup
- `C:\source\vincedevwork\TimePunchClock\src\TimeApi\Api\TimePunchController.cs` - API endpoints
- `C:\source\vincedevwork\TimePunchClock\.github\workflows\frontend-deploy-dev.yml` - Deployment workflow
