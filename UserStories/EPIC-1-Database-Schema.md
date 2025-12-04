# Epic 1: Database Schema Migration

## Goal
Update the database schema to support multi-user authentication while maintaining backwards compatibility with existing punch records.

---

## US-101: Add AuthId and WorkDescription Columns to Punchs Table

**As a** developer
**I want** to add AuthId and WorkDescription columns to the Punchs table
**So that** punch records can be associated with specific users and include work details

### Acceptance Criteria
- [ ] Add `AuthId` column to `PunchEntity.cs` (nullable string, max 255 chars)
- [ ] Add `WorkDescription` column to `PunchEntity.cs` (nullable string, max 1000 chars)
- [ ] Update `TimeClockDbContext.cs` to include the new columns in the model
- [ ] Both columns should be nullable to support legacy records and optional usage
- [ ] Add index on AuthId for query performance
- [ ] Existing records should have NULL AuthId and NULL WorkDescription after migration

### Technical Notes
```csharp
public string? AuthId { get; set; } // Azure AD Object ID
public string? WorkDescription { get; set; } // Description of work performed
```

### Files to Modify
- `src/TimeApi/Models/PunchEntity.cs`
- `src/TimeApi/Services/TimeClockDbContext.cs`

---

## US-102: Create Database Migration Script

**As a** developer
**I want** a migration script to update existing databases
**So that** deployed environments can be updated without data loss

### Acceptance Criteria
- [ ] Create EF Core migration using `Add-Migration AddAuthIdAndWorkDescription`
- [ ] Migration should add AuthId column as nullable
- [ ] Migration should add WorkDescription column as nullable
- [ ] Migration should add index on AuthId
- [ ] Test migration on local SQL Server container
- [ ] Document manual SQL script alternative for production

### Technical Notes
- First migration for this project (move away from `EnsureCreated()`)
- Migration script should be idempotent

### Files to Create
- `src/TimeApi/Migrations/[timestamp]_AddAuthIdAndWorkDescription.cs`

---

## US-103: Update DTOs for AuthId and WorkDescription Support

**As a** developer
**I want** to update shared DTOs to include AuthId and WorkDescription
**So that** client and server can exchange user identity and work details

### Acceptance Criteria
- [ ] Add `AuthId` property to `PunchRecord` DTO (nullable)
- [ ] Add `WorkDescription` property to `PunchRecord` DTO (nullable)
- [ ] Add `WorkDescription` property to `PunchInfo` DTO (nullable) for user input
- [ ] Ensure serialization/deserialization works correctly
- [ ] Update any mapping logic in repository layer
- [ ] Verify backwards compatibility with existing API contracts

### Technical Notes
- AuthId should NOT be required in request DTOs (set from token)
- AuthId should be included in response DTOs
- WorkDescription is optional and can be provided by user when punching in/out

### Files to Modify
- `src/TimeClock.client/Class1.cs` (PunchRecord and PunchInfo DTOs)
- `src/TimeApi/Services/TimePunchRepository.cs` (mapping logic)

---

## Definition of Done (Epic 1)
- [ ] All unit tests pass
- [ ] Migration runs successfully on local SQL Server
- [ ] Existing punch records remain intact with NULL AuthId and NULL WorkDescription
- [ ] New punches can be created with AuthId and optional WorkDescription populated
- [ ] API still returns all records when AuthId is NULL (backwards compat)
- [ ] WorkDescription displays correctly in UI when present
- [ ] Code reviewed and merged to main branch
