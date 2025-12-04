# Epic 8: Testing & Validation

## Goal
Perform end-to-end testing of authentication, authorization, and deployment workflows to ensure the complete system works as expected.

---

## US-801: End-to-End Authentication Flow Testing

**As a** QA engineer
**I want** to verify the complete authentication flow
**So that** users can successfully log in and access their data

### Acceptance Criteria
- [ ] Test user login with Azure AD credentials
- [ ] Verify JWT token is obtained and stored
- [ ] Verify token is included in API requests
- [ ] Test token refresh when expired
- [ ] Verify logout clears authentication state
- [ ] Test behavior when token is invalid
- [ ] Test behavior when token is missing
- [ ] Document test scenarios and results

### Test Scenarios
1. New user logs in for the first time
2. Existing user logs in with valid credentials
3. User logs out and logs back in
4. Token expires during active session
5. User with invalid token attempts API call
6. User denies consent on Azure AD screen

### Files to Create
- `tests/e2e/auth-flow.test.md` (manual test script)
- Or `tests/e2e/AuthFlowTests.cs` (automated tests)

---

## US-802: Authorization and Data Isolation Testing

**As a** QA engineer
**I want** to verify users can only access their own punch records
**So that** data isolation is properly enforced

### Acceptance Criteria
- [ ] Create two test users in Azure AD
- [ ] User A creates punch records
- [ ] User B creates punch records
- [ ] Verify User A only sees their own records
- [ ] Verify User B only sees their own records
- [ ] Test that users cannot modify each other's records
- [ ] Verify legacy records (AuthId = NULL) visible to all users
- [ ] Document test results

### Test Scenarios
1. User A punches in/out several times
2. User B punches in/out several times
3. User A calls GET /api/TimePunch - should only see User A's records
4. User B calls GET /api/TimePunch - should only see User B's records
5. User A tries to punch out User B's open punch - should fail
6. Both users can see legacy records with NULL AuthId

### Files to Create
- `tests/integration/AuthorizationTests.cs`
- `tests/e2e/data-isolation.test.md`

---

## US-803: Infrastructure Deployment Validation

**As a** DevOps engineer
**I want** to validate that infrastructure deploys correctly
**So that** applications have the resources they need

### Acceptance Criteria
- [ ] Deploy dev infrastructure via GitHub Actions
- [ ] Verify all Azure resources created (Resource Group, SQL Server, SQL DB, ACR, Container Instance, Static Web App, Key Vault)
- [ ] Verify SQL admin password stored in Key Vault
- [ ] Verify connection string stored in Key Vault
- [ ] Verify Container Instance can access Key Vault
- [ ] Verify SQL Database has correct schema (Punchs table with AuthId)
- [ ] Deploy prod infrastructure with approval workflow
- [ ] Verify prod resources isolated from dev
- [ ] Document any issues encountered

### Files to Create
- `Infra/docs/deployment-validation-checklist.md`

---

## US-804: Full Stack Deployment and Smoke Tests

**As a** DevOps engineer
**I want** automated smoke tests after deployment
**So that** we know if deployment succeeded

### Acceptance Criteria
- [ ] Deploy complete stack to dev via GitHub Actions
- [ ] Run health check on API endpoint (GET /health or similar)
- [ ] Verify frontend loads successfully
- [ ] Verify database migrations applied
- [ ] Test sample API call with authentication
- [ ] Verify Azure AD login page loads
- [ ] Create smoke test script that runs post-deployment
- [ ] Add smoke tests to deployment workflows
- [ ] Fail deployment if smoke tests fail

### Technical Notes
- Smoke tests should be quick (< 2 minutes)
- Use curl or automated browser tests (Playwright/Selenium)

### Files to Create
- `tests/smoke/smoke-tests.sh`
- `tests/smoke/SmokeTests.cs`

---

## Definition of Done (Epic 8)
- [ ] Authentication flow tested end-to-end
- [ ] Authorization verified with multiple users
- [ ] Data isolation confirmed between users
- [ ] Legacy records (AuthId = NULL) accessible to all users
- [ ] Infrastructure deployments validated in dev and prod
- [ ] Smoke tests pass after deployment
- [ ] All test results documented
- [ ] Known issues logged with mitigation plans
- [ ] System ready for production use
