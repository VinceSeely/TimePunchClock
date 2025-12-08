# Azure AD JWT Authentication Troubleshooting Guide

## Problem: IDX10500 Signature Validation Failed

### Error Details
```
IDX10500: Signature validation failed. No security keys were provided to validate the signature.
```

This error occurs when the ASP.NET Core JWT middleware cannot retrieve the signing keys from Azure AD's metadata endpoint.

---

## Root Causes Identified and Fixed

### 1. Missing Explicit MetadataAddress Configuration (PRIMARY FIX)
**Problem**: In Azure Container Apps, automatic metadata discovery can fail due to DNS resolution or network connectivity issues.

**Solution Applied** (Lines 59-76 in Program.cs):
```csharp
// CRITICAL FIX: Explicitly set MetadataAddress for Azure AD
if (!isUsingIdentityServer && !string.IsNullOrEmpty(tenantId))
{
    options.MetadataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";

    // Increase timeout for metadata retrieval in containerized environments
    options.BackchannelTimeout = TimeSpan.FromSeconds(30);

    // Force HTTPS for Azure AD metadata (security best practice)
    options.RequireHttpsMetadata = true;
}
```

**Why This Works**:
- Explicitly sets the metadata URL instead of relying on automatic construction
- Increases the timeout from default 60s to 30s for the backchannel HTTP client
- Ensures HTTPS is required for Azure AD (security best practice)

### 2. Development Environment HTTPS Metadata Issue
**Problem**: The original code disabled HTTPS metadata in Development mode, which could cause issues when connecting to Azure AD (which requires HTTPS).

**Solution Applied** (Lines 45-47, 72-76):
```csharp
var isUsingIdentityServer = !string.IsNullOrEmpty(identityServerAuthority) &&
                           builder.Environment.IsDevelopment() &&
                           builder.Configuration.GetValue<bool>("Authentication:UseIdentityServer", false);

// Later...
else if (isUsingIdentityServer)
{
    // For local IdentityServer development only
    options.RequireHttpsMetadata = false;
}
```

**Why This Works**:
- Only disables HTTPS metadata when explicitly using local IdentityServer
- Azure AD always uses HTTPS, regardless of environment
- Adds an explicit flag `Authentication:UseIdentityServer` for clarity

### 3. Enhanced Logging for Diagnostics
**Solution Applied** (Lines 109-142):
Added comprehensive JWT event logging including:
- `OnMessageReceived`: Log when authorization header is received
- `OnAuthenticationFailed`: Log detailed exception information
- `OnTokenValidated`: Log successful validation with claims
- `OnChallenge`: Log challenge details

---

## Debugging Steps

### Step 1: Verify Configuration
Ensure your Azure Container Apps environment variables are set:

```bash
# Required Azure AD Configuration
AzureAd__TenantId=bbe45b4e-b07a-4b69-ae5a-54880360f7d0
AzureAd__Authority=https://login.microsoftonline.com/bbe45b4e-b07a-4b69-ae5a-54880360f7d0/v2.0
AzureAd__ClientId=<your-api-client-id>
AzureAd__Audience=api://<your-api-client-id>

# Environment
ASPNETCORE_ENVIRONMENT=Development  # or Production
Authentication__Enabled=true
```

### Step 2: Test Azure AD Connectivity
Use the new diagnostics endpoint to verify connectivity:

```bash
# Health check (no auth required)
curl https://your-api.azurecontainerapps.io/api/diagnostics/health

# Test Azure AD metadata connectivity
curl https://your-api.azurecontainerapps.io/api/diagnostics/azure-ad-connectivity

# View current auth configuration
curl https://your-api.azurecontainerapps.io/api/diagnostics/auth-config
```

**Expected Response** (azure-ad-connectivity):
```json
{
  "success": true,
  "metadataUrl": "https://login.microsoftonline.com/bbe45b4e.../v2.0/.well-known/openid-configuration",
  "responseTime": "245ms",
  "hasJwksUri": true,
  "jwksUri": "https://login.microsoftonline.com/bbe45b4e.../discovery/v2.0/keys",
  "jwksResponseTime": "198ms",
  "jwksRetrieved": true,
  "jwksKeyCount": 3,
  "message": "Successfully connected to Azure AD metadata endpoint"
}
```

### Step 3: Check Container App Logs
View the enhanced authentication logs in Azure Container Apps:

```bash
# Using Azure CLI
az containerapp logs show --name <your-app-name> --resource-group <your-rg> --follow

# Look for these log patterns:
# [AUTH] Received authorization header (length: 847)
# [AUTH SUCCESS] Token validated for: user@domain.com
# [AUTH ERROR] Authentication failed: SecurityTokenSignatureKeyNotFoundException
```

### Step 4: Test with a Valid Token
```bash
# Get a token from Azure AD
# (Use your frontend login flow or Azure CLI)

# Test the authenticated endpoint
curl -H "Authorization: Bearer <your-token>" \
  https://your-api.azurecontainerapps.io/api/diagnostics/test-auth
```

---

## Common Issues and Solutions

### Issue: Timeout Errors
**Symptom**: Gateway timeout (504) when calling the API

**Solution**:
- Check Container App outbound connectivity
- Verify no firewall rules blocking traffic to login.microsoftonline.com
- Increase `BackchannelTimeout` if needed (currently 30s)

### Issue: Wrong Tenant ID
**Symptom**: Token validation fails with issuer mismatch

**Solution**:
```csharp
ValidIssuers = new[]
{
    $"https://login.microsoftonline.com/{tenantId}/v2.0",
    $"https://sts.windows.net/{tenantId}/"
}
```
Both issuer formats are accepted in the configuration.

### Issue: Audience Mismatch
**Symptom**: "The audience 'api://xxx' is invalid"

**Solution**:
The configuration accepts both audience formats:
- `api://<client-id>` (API identifier URI)
- `<client-id>` (Just the GUID)

```csharp
ValidAudiences = new[] { audience, clientId }.Where(a => !string.IsNullOrEmpty(a))
```

### Issue: Network Connectivity from Container Apps
**Symptom**: Cannot reach login.microsoftonline.com

**Troubleshooting**:
1. Test DNS resolution inside the container:
   ```bash
   # Exec into container
   nslookup login.microsoftonline.com
   ```

2. Test HTTPS connectivity:
   ```bash
   curl -v https://login.microsoftonline.com
   ```

3. Check Container App network configuration:
   - Verify no VNet restrictions
   - Check NSG rules if using VNet integration
   - Verify no firewall blocking outbound HTTPS

---

## Verification Checklist

- [ ] Environment variables are correctly set in Azure Container Apps
- [ ] TenantId is correct and matches your Azure AD tenant
- [ ] ClientId matches your API app registration in Azure AD
- [ ] Audience is configured (either api://clientid or just clientid)
- [ ] `/api/diagnostics/azure-ad-connectivity` returns success
- [ ] Container App can reach login.microsoftonline.com (port 443)
- [ ] Logs show "[AUTH SUCCESS]" when sending valid tokens
- [ ] No "[AUTH ERROR] IDX10500" errors in logs

---

## Additional Resources

### Azure AD Metadata Endpoints
- **Discovery Document**: `https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration`
- **JWKS (Signing Keys)**: `https://login.microsoftonline.com/{tenantId}/discovery/v2.0/keys`

### Microsoft Documentation
- [Azure AD Authentication in ASP.NET Core](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-v2-aspnet-core-webapp)
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [Container Apps Networking](https://learn.microsoft.com/en-us/azure/container-apps/networking)

### Key Configuration Properties
```csharp
JwtBearerOptions {
    Authority,              // Base authority URL
    MetadataAddress,        // Explicit metadata endpoint (CRITICAL)
    RequireHttpsMetadata,   // Enforce HTTPS (true for Azure AD)
    BackchannelTimeout,     // Timeout for HTTP requests (30s)
    TokenValidationParameters {
        ValidateIssuer,
        ValidateAudience,
        ValidateLifetime,
        ValidateIssuerSigningKey,
        ValidIssuers,
        ValidAudiences
    }
}
```

---

## Performance Considerations

### Metadata Caching
The JWT middleware automatically caches the metadata and signing keys. Default settings:
- **Metadata refresh interval**: 24 hours
- **Key refresh interval**: 24 hours

To customize:
```csharp
options.MetadataAddress = "...";
options.RefreshInterval = TimeSpan.FromHours(12);  // Custom refresh
```

### Startup Performance
First request after app start will fetch metadata (200-500ms penalty). Subsequent requests use cached keys.

**Optimization**: Consider using Azure Container Apps warm-up feature or health probes to trigger metadata fetch during startup.

---

## Security Best Practices

1. **Always use HTTPS** for Azure AD metadata (enforced in code)
2. **Validate all token properties**: issuer, audience, lifetime, signature
3. **Use specific audiences**: Prefer `api://clientid` over wildcards
4. **Log authentication failures**: But don't log full tokens (security risk)
5. **Rotate keys regularly**: Azure AD does this automatically; middleware handles it
6. **Use managed identities**: For Azure resource access (separate from user auth)

---

## Next Steps After Fix

1. **Redeploy the container** with updated Program.cs
2. **Test the diagnostics endpoints** to verify connectivity
3. **Monitor the logs** for authentication events
4. **Test with real tokens** from your frontend application
5. **Consider moving to Production environment** once validated

---

## Contact and Support

If issues persist after applying these fixes:
1. Check Azure Container Apps service health
2. Verify Azure AD tenant is active
3. Review app registration configuration in Azure Portal
4. Check for any Azure AD conditional access policies
5. Contact Azure Support if network connectivity issues persist
