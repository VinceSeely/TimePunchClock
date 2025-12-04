# Epic 3: Blazor WASM Frontend Authentication

## Goal
Implement Azure AD login flow, JWT token management, and authenticated API calls in the Blazor WebAssembly application.

---

## US-301: Configure Azure AD Authentication in Blazor WASM

**As a** developer
**I want** to configure Azure AD authentication in the Blazor WASM app
**So that** users can log in with their Microsoft accounts

### Acceptance Criteria
- [ ] Install `Microsoft.Authentication.WebAssembly.Msal` NuGet package
- [ ] Configure MSAL authentication in `Program.cs`
- [ ] Add Azure AD settings to `wwwroot/appsettings.json`
- [ ] Configure scopes for API access
- [ ] Add authentication state provider
- [ ] Test login/logout flow

### Technical Notes
```json
"AzureAd": {
  "Authority": "https://login.microsoftonline.com/<tenant-id>",
  "ClientId": "<blazor-client-id>",
  "ValidateAuthority": true
}
```

### Files to Modify
- `src/TimeClockUI/TimeClockUI.csproj` (add package)
- `src/TimeClockUI/Program.cs` (add MSAL services)
- `src/TimeClockUI/wwwroot/appsettings.json` (add config)

---

## US-302: Add Login/Logout UI Components

**As a** user
**I want** to see login and logout buttons
**So that** I can authenticate with my Microsoft account

### Acceptance Criteria
- [ ] Add `LoginDisplay.razor` component showing user name when logged in
- [ ] Add Login button that redirects to Microsoft login page
- [ ] Add Logout button that clears authentication state
- [ ] Display component in `MainLayout.razor` header
- [ ] Show loading state during authentication
- [ ] Handle authentication errors gracefully

### Technical Notes
- Use `AuthorizeView` component for conditional rendering
- Display user's email or name from claims

### Files to Create
- `src/TimeClockUI/Shared/LoginDisplay.razor`

### Files to Modify
- `src/TimeClockUI/Layout/MainLayout.razor`

---

## US-303: Protect Home Page with Authorization

**As a** developer
**I want** to require authentication for the Home page
**So that** unauthenticated users cannot access the punch clock

### Acceptance Criteria
- [ ] Add `@attribute [Authorize]` to `Home.razor`
- [ ] Configure `App.razor` to show login prompt for unauthorized access
- [ ] Add `Authentication.razor` page for handling auth callbacks
- [ ] Test that unauthenticated users are redirected to login
- [ ] Verify authenticated users can access Home page

### Technical Notes
- Use `<CascadingAuthenticationState>` in App.razor
- Add routes for authentication callbacks

### Files to Modify
- `src/TimeClockUI/Pages/Home.razor`
- `src/TimeClockUI/App.razor`

### Files to Create
- `src/TimeClockUI/Pages/Authentication.razor`

---

## US-304: Configure HTTP Client to Send JWT Tokens

**As a** developer
**I want** API requests to automatically include JWT bearer tokens
**So that** the API can authenticate and authorize the user

### Acceptance Criteria
- [ ] Configure `HttpClient` with `AuthorizationMessageHandler`
- [ ] Automatically attach access token to API requests
- [ ] Configure authorized URLs (API base URL)
- [ ] Configure required scopes for API access
- [ ] Handle token refresh automatically
- [ ] Handle 401 responses by prompting re-login

### Technical Notes
```csharp
builder.Services.AddHttpClient("TimeClockAPI", client =>
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
```

### Files to Modify
- `src/TimeClockUI/Program.cs`
- `src/TimeClock.client/Class1.cs` (update TimePunchClient to use named client)

---

## US-305: Update TimePunchClient for Authenticated Calls

**As a** developer
**I want** the TimePunchClient to use authenticated HTTP client
**So that** all API calls include proper authentication

### Acceptance Criteria
- [ ] Update `TimePunchClient` constructor to accept `IHttpClientFactory`
- [ ] Use named HTTP client configured with auth handler
- [ ] Remove any hardcoded base URLs (use configuration)
- [ ] Test all client methods with authenticated requests
- [ ] Handle token expiration gracefully

### Technical Notes
- May need to refactor from static HttpClient to injected IHttpClientFactory
- Ensure all methods use the authenticated client

### Files to Modify
- `src/TimeClock.client/Class1.cs`
- `src/TimeClockUI/Program.cs` (DI registration)

---

## Definition of Done (Epic 3)
- [ ] Users can log in with Microsoft accounts
- [ ] Home page requires authentication
- [ ] Logout clears authentication state
- [ ] API calls include JWT bearer tokens
- [ ] Token refresh works automatically
- [ ] Authentication errors display user-friendly messages
- [ ] UI shows user's name/email when logged in
- [ ] Code reviewed and merged to main branch
