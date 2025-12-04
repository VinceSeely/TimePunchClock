# TimeClock Authentication Implementation - User Stories Overview

## Project Goal
Implement Azure AD/Entra ID authentication for the TimeClock Blazor WASM application with JWT tokens, update database schema to support multi-user data with work descriptions, establish comprehensive testing infrastructure, and establish CI/CD pipelines for infrastructure and application deployment.

## Key Requirements
- **Testing**: Test-driven development with unit and integration tests
- **Authentication**: Azure AD/Entra ID with JWT tokens
- **Authorization**: RBAC - Users can only view/modify their own punch records
- **Work Descriptions**: Optional field to track what users worked on
- **Backwards Compatibility**: Records without AuthId return all data (legacy support)
- **User Management**: No admin UI initially - users managed in Azure AD
- **Infrastructure**: Terraform with random password generation, separate dev/prod resource groups
- **Deployment**: GitHub Actions with manual approval for prod promotion

## Epics

### Epic 0: Testing Infrastructure Setup ⚠️ **START HERE**
Establish comprehensive testing infrastructure before implementing new features.

**Stories**: US-001 through US-008

**CRITICAL**: This epic must be completed FIRST to enable test-driven development.

---

### Epic 1: Database Schema Migration
Update database schema to support multi-user authentication and work descriptions with backwards compatibility.

**Stories**: US-101 through US-103

---

### Epic 2: Backend API Authentication & Authorization
Implement JWT token validation, Azure AD integration, and user-scoped data access in the API.

**Stories**: US-201 through US-206

---

### Epic 3: Blazor WASM Frontend Authentication
Implement Azure AD login, JWT token management, and authenticated API calls in the frontend.

**Stories**: US-301 through US-305

---

### Epic 4: Infrastructure as Code - Dev Environment
Update Terraform configuration for dev environment with proper secrets management and Azure AD integration.

**Stories**: US-401 through US-405

---

### Epic 5: Infrastructure as Code - Prod Environment
Create production Terraform configuration with separate state management.

**Stories**: US-501 through US-503

---

### Epic 6: CI/CD Pipeline - Infrastructure
Implement GitHub Actions workflows for Terraform deployment and promotion.

**Stories**: US-601 through US-604

---

### Epic 7: CI/CD Pipeline - Applications
Implement GitHub Actions workflows for frontend and backend deployment.

**Stories**: US-701 through US-705

---

### Epic 8: Testing & Validation
End-to-end testing of authentication, authorization, and deployment workflows.

**Stories**: US-801 through US-804

---

### Epic 9: UI Enhancements for Work Description
Add UI components to allow users to optionally enter and view work descriptions.

**Stories**: US-901 through US-904

---

## Implementation Order

**Phase 0: Testing Foundation (Epic 0)** ⚠️
0. **Testing infrastructure setup (Epic 0) - MUST BE COMPLETED FIRST**

**Phase 1: Local Development (Epics 1-3, 9)**
1. Database schema changes (Epic 1)
2. Backend API auth (Epic 2)
3. Frontend auth (Epic 3)
4. UI enhancements for work description (Epic 9)

**Phase 2: Infrastructure Setup (Epics 4-5)**
5. Dev infrastructure (Epic 4)
6. Prod infrastructure (Epic 5)

**Phase 3: Automation (Epics 6-7)**
7. Infrastructure CI/CD (Epic 6)
8. Application CI/CD (Epic 7)

**Phase 4: Validation (Epic 8)**
9. End-to-end testing (Epic 8)

---

## Dependencies
- Azure AD/Entra ID tenant (must exist)
- Azure subscription with appropriate permissions
- GitHub repository with Actions enabled
- Service Principal for GitHub Actions

## Out of Scope
- Admin UI for user management
- Password reset flows (handled by Azure AD)
- Staging environment
- User profile management
- Audit logging (future enhancement)
- Advanced work description features (rich text, attachments, etc.)

---

## Epic Summary

| Epic # | Name | Stories | Priority |
|--------|------|---------|----------|
| 0 | Testing Infrastructure Setup | 8 (US-001 to US-008) | **CRITICAL - START HERE** |
| 1 | Database Schema Migration | 3 (US-101 to US-103) | High |
| 2 | Backend API Authentication & Authorization | 6 (US-201 to US-206) | High |
| 3 | Blazor WASM Frontend Authentication | 5 (US-301 to US-305) | High |
| 4 | Infrastructure as Code - Dev Environment | 5 (US-401 to US-405) | Medium |
| 5 | Infrastructure as Code - Prod Environment | 3 (US-501 to US-503) | Medium |
| 6 | CI/CD Pipeline - Infrastructure | 4 (US-601 to US-604) | Medium |
| 7 | CI/CD Pipeline - Applications | 6 (US-701 to US-706) | Medium |
| 8 | Testing & Validation | 4 (US-801 to US-804) | High |
| 9 | UI Enhancements for Work Description | 4 (US-901 to US-904) | Low |
| **Total** | **10 Epics** | **48 Stories** | |

---

## Testing Strategy

### Test Coverage Requirements
- **Minimum Coverage**: 70% for all code
- **Target Coverage**: 80%+ for business logic
- **Approach**: Test-Driven Development (TDD)

### Testing Principles
1. **Write tests BEFORE implementation** for all new features
2. **Update tests BEFORE modifying** existing code
3. **Never commit code** without corresponding tests
4. **Block PR merges** if tests fail or coverage drops

### Test Types
- **Unit Tests**: Business logic, controllers, services, repositories
- **Integration Tests**: Database interactions, API endpoints
- **Component Tests**: Blazor components (using bUnit)
- **E2E Tests**: Complete user workflows (manual initially)
- **Smoke Tests**: Post-deployment validation
