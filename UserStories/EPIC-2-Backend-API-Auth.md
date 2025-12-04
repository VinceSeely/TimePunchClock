# Epic 2: Backend API Authentication & Authorization

## Goal
Implement JWT token validation, Azure AD integration, and user-scoped data access control in the ASP.NET Core API.

---

## US-201: Configure Azure AD Authentication in API

**As a** developer
**I want** to configure Azure AD JWT bearer authentication in the API
**So that** only authenticated users with valid tokens can access endpoints

### Acceptance Criteria
- [ ] Install `Microsoft.Identity.Web` NuGet package
- [ ] Configure JWT bearer authentication in `Program.cs`
- [ ] Add Azure AD settings to `appsettings.json` (TenantId, ClientId, Instance)
- [ ] Configure token validation parameters
- [ ] Add authentication middleware to pipeline
- [ ] Test with valid and invalid tokens

### Technical Notes
```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "TenantId": "<tenant-id>",
  "ClientId": "<api-client-id>",
  "Audience": "api://<api-client-id>"
}
```

### Files to Modify
- `src/TimeApi/TimeApi.csproj` (add package)
- `src/TimeApi/Program.cs` (add auth services)
- `src/TimeApi/appsettings.json` (add config)
- `src/TimeApi/appsettings.Development.json` (dev settings)

---

## US-202: Add Authorization Attributes to TimePunch Controller

**As a** developer
**I want** to protect all TimePunch endpoints with [Authorize] attributes
**So that** unauthenticated requests are rejected

### Acceptance Criteria
- [ ] Add `[Authorize]` attribute to `TimePunchController` class
- [ ] Verify all endpoints require authentication
- [ ] Test that 401 Unauthorized is returned for missing/invalid tokens
- [ ] Ensure OPTIONS requests (CORS preflight) still work

### Technical Notes
- Class-level `[Authorize]` applies to all actions
- May need to configure CORS to work with auth

### Files to Modify
- `src/TimeApi/Api/TimePunchController.cs`
- `src/TimeApi/Program.cs` (CORS configuration)

---

## US-203: Extract AuthId from JWT Claims

**As a** developer
**I want** to extract the user's AuthId from JWT token claims
**So that** I can associate punch records with the authenticated user

### Acceptance Criteria
- [ ] Create helper method to extract AuthId from `HttpContext.User`
- [ ] Use Azure AD Object ID claim (`oid` or `sub`)
- [ ] Handle missing claim gracefully with appropriate error
- [ ] Log the extracted AuthId for debugging
- [ ] Create base controller or service for claim extraction

### Technical Notes
```csharp
var authId = User.FindFirst("oid")?.Value
    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

### Files to Modify
- `src/TimeApi/Api/TimePunchController.cs` (add helper method)
- Or create `src/TimeApi/Services/ICurrentUserService.cs` interface

---

## US-204: Update Repository to Filter by AuthId

**As a** developer
**I want** the repository to filter punch records by AuthId
**So that** users only see their own data

### Acceptance Criteria
- [ ] Update `GetPunchRecords()` to accept optional AuthId parameter
- [ ] Update `GetLastPunch()` to accept optional AuthId parameter
- [ ] If AuthId is NULL/empty, return all records (backwards compatibility)
- [ ] If AuthId is provided, filter WHERE AuthId = @authId OR AuthId IS NULL
- [ ] Update LINQ queries with proper filtering
- [ ] Add unit tests for filtering logic

### Technical Notes
- Legacy records (AuthId = NULL) should be visible to all users initially
- Consider adding admin role later to view all records

### Files to Modify
- `src/TimeApi/Services/TimePunchRepository.cs`
- `src/TimeApi/Services/ITimePunchRepository.cs` (interface)

---

## US-205: Update InsertPunch to Set AuthId

**As a** developer
**I want** new punch records to automatically include the authenticated user's AuthId
**So that** records are properly scoped to users

### Acceptance Criteria
- [ ] Modify `InsertPunch()` to accept AuthId parameter
- [ ] Set `AuthId` on new `PunchEntity` before saving
- [ ] Ensure auto-close logic only closes punches for same AuthId
- [ ] Prevent users from punching out other users' records
- [ ] Add validation to ensure AuthId is provided for new records

### Technical Notes
- When closing previous punch, add filter: `WHERE AuthId = @authId AND PunchOut IS NULL`

### Files to Modify
- `src/TimeApi/Services/TimePunchRepository.cs`

---

## US-206: Update Controller Actions to Pass AuthId

**As a** developer
**I want** all controller actions to extract and pass AuthId to repository
**So that** authorization rules are enforced consistently

### Acceptance Criteria
- [ ] Update `GetPunchRecords()` action to extract AuthId and pass to repository
- [ ] Update `Punch()` action to extract AuthId and pass to repository
- [ ] Update `GetLastPunch()` action to extract AuthId and pass to repository
- [ ] Handle null/missing AuthId with 401 Unauthorized response
- [ ] Add integration tests for each endpoint with auth

### Technical Notes
- All actions should follow same pattern: extract AuthId → call repository with AuthId
- Consider using action filter for automatic AuthId injection

### Files to Modify
- `src/TimeApi/Api/TimePunchController.cs`

---

## Definition of Done (Epic 2)
- [ ] API rejects requests without valid JWT tokens (401)
- [ ] Users can only create punch records for themselves
- [ ] Users can only view their own punch records
- [ ] Legacy records (AuthId = NULL) are returned to all users
- [ ] Integration tests pass with authenticated requests
- [ ] API documentation updated with auth requirements
- [ ] Code reviewed and merged to main branch
