# Epic 0: Testing Infrastructure Setup

## Goal
Establish comprehensive testing infrastructure including unit tests, integration tests, and test-driven development practices before implementing new features.

**Note**: This epic should be completed BEFORE starting Epic 1 to ensure tests are written alongside code changes.

---

## US-001: Create Unit Test Project for API

**As a** developer
**I want** a unit test project for the TimeApi
**So that** I can write tests for business logic and ensure code quality

### Acceptance Criteria
- [ ] Create `TimeApi.Tests` project using xUnit
- [ ] Add reference to `TimeApi` project
- [ ] Install necessary NuGet packages:
  - xUnit (2.4.2+)
  - xUnit.runner.visualstudio
  - Microsoft.NET.Test.Sdk
  - Moq (4.20.0+) for mocking
  - FluentAssertions (6.12.0+) for readable assertions
- [ ] Create folder structure: `Controllers/`, `Services/`, `Repositories/`
- [ ] Add test example to verify setup works
- [ ] Configure test project in solution file
- [ ] Add to CI/CD pipeline

### Technical Notes
```bash
dotnet new xunit -n TimeApi.Tests -o tests/TimeApi.Tests
dotnet add tests/TimeApi.Tests reference src/TimeApi/TimeApi.csproj
dotnet add tests/TimeApi.Tests package Moq
dotnet add tests/TimeApi.Tests package FluentAssertions
```

### Files to Create
- `tests/TimeApi.Tests/TimeApi.Tests.csproj`
- `tests/TimeApi.Tests/Controllers/TimePunchControllerTests.cs` (example)
- `tests/TimeApi.Tests/Services/TimePunchRepositoryTests.cs` (example)

---

## US-002: Create Integration Test Project for API

**As a** developer
**I want** an integration test project for the TimeApi
**So that** I can test database interactions and end-to-end API flows

### Acceptance Criteria
- [ ] Create `TimeApi.IntegrationTests` project using xUnit
- [ ] Install necessary NuGet packages:
  - Microsoft.AspNetCore.Mvc.Testing
  - Microsoft.EntityFrameworkCore.InMemory
  - Testcontainers.MsSql (for real SQL Server container tests)
- [ ] Create `WebApplicationFactory<Program>` for API testing
- [ ] Configure in-memory database for fast tests
- [ ] Create test fixtures for database setup/teardown
- [ ] Add example integration test for GET endpoint
- [ ] Configure parallel test execution settings

### Technical Notes
```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace SQL Server with in-memory database
            services.AddDbContext<TimeClockDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        });
    }
}
```

### Files to Create
- `tests/TimeApi.IntegrationTests/TimeApi.IntegrationTests.csproj`
- `tests/TimeApi.IntegrationTests/CustomWebApplicationFactory.cs`
- `tests/TimeApi.IntegrationTests/Controllers/TimePunchControllerIntegrationTests.cs`

---

## US-003: Write Tests for Existing TimePunchRepository

**As a** developer
**I want** comprehensive tests for the existing TimePunchRepository
**So that** I don't break existing functionality when adding authentication

### Acceptance Criteria
- [ ] Write unit tests for `InsertPunch()` method
  - Test punch in creates new record
  - Test punch out closes existing record
  - Test auto-close previous punch when punching in
  - Test handling of null/invalid inputs
- [ ] Write unit tests for `GetPunchRecords()` method
  - Test date range filtering
  - Test empty results
  - Test multiple records
- [ ] Write unit tests for `GetLastPunch()` method
  - Test returns most recent punch
  - Test returns null when no punches exist
- [ ] Achieve 80%+ code coverage for repository
- [ ] Use in-memory database for isolation

### Technical Notes
- Mock `TimeClockDbContext` or use in-memory provider
- Existing code has no tests, so this establishes baseline

### Files to Create
- `tests/TimeApi.Tests/Services/TimePunchRepositoryTests.cs`

---

## US-004: Write Tests for Existing TimePunchController

**As a** developer
**I want** tests for the existing TimePunchController
**So that** I can safely add authentication logic

### Acceptance Criteria
- [ ] Write unit tests for GET /api/TimePunch endpoint
  - Test returns punch records for date range
  - Test handles invalid date parameters
- [ ] Write unit tests for POST /api/TimePunch endpoint
  - Test creates punch in
  - Test creates punch out
  - Test validates PunchInfo input
- [ ] Write unit tests for GET /api/TimePunch/lastpunch endpoint
  - Test returns last punch
  - Test handles no punches case
- [ ] Mock `ITimePunchRepository` dependency
- [ ] Achieve 80%+ code coverage for controller

### Technical Notes
```csharp
[Fact]
public async Task GetPunchRecords_ReturnsOk_WithValidDateRange()
{
    // Arrange
    var mockRepo = new Mock<ITimePunchRepository>();
    mockRepo.Setup(r => r.GetPunchRecords(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
        .ReturnsAsync(new List<PunchRecord>());
    var controller = new TimePunchController(mockRepo.Object);

    // Act
    var result = await controller.GetPunchRecords(DateTime.Now, DateTime.Now);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
}
```

### Files to Create
- `tests/TimeApi.Tests/Controllers/TimePunchControllerTests.cs`

---

## US-005: Create Unit Test Project for Blazor UI

**As a** developer
**I want** a unit test project for the Blazor frontend
**So that** I can test component logic and user interactions

### Acceptance Criteria
- [ ] Create `TimeClockUI.Tests` project using bUnit
- [ ] Install necessary NuGet packages:
  - bUnit (1.25.0+)
  - bUnit.web
  - xUnit
  - Moq
- [ ] Create test base class with common setup
- [ ] Add example component test for Home.razor
- [ ] Configure mock HttpClient for API calls
- [ ] Test punch button click behavior
- [ ] Test hour type selection

### Technical Notes
```csharp
using Bunit;
using Xunit;

public class HomePageTests : TestContext
{
    [Fact]
    public void HomePage_RendersCorrectly()
    {
        // Arrange
        var cut = RenderComponent<Home>();

        // Assert
        cut.Find("h3").TextContent.Should().Contain("Time Clock");
    }
}
```

### Files to Create
- `tests/TimeClockUI.Tests/TimeClockUI.Tests.csproj`
- `tests/TimeClockUI.Tests/Pages/HomeTests.cs`
- `tests/TimeClockUI.Tests/TestBase.cs`

---

## US-006: Configure Code Coverage Reporting

**As a** developer
**I want** automated code coverage reporting
**So that** I can track test coverage over time

### Acceptance Criteria
- [ ] Install coverlet.collector in all test projects
- [ ] Configure code coverage in test execution
- [ ] Generate coverage reports in XML and HTML formats
- [ ] Set minimum coverage threshold (70%)
- [ ] Add coverage badge to README (optional)
- [ ] Configure CI pipeline to fail if coverage drops below threshold
- [ ] Publish coverage reports as build artifacts

### Technical Notes
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

### Files to Create
- `.github/workflows/test-coverage.yml`
- `tests/coverage.runsettings`

---

## US-007: Setup Test Data Builders and Factories

**As a** developer
**I want** test data builders for common entities
**So that** tests are easier to write and maintain

### Acceptance Criteria
- [ ] Create `PunchEntityBuilder` for test data generation
- [ ] Create `PunchRecordBuilder` for DTO test data
- [ ] Create `TestDataFactory` with common scenarios
- [ ] Support fluent API for customization
- [ ] Include realistic test data (dates, types, etc.)
- [ ] Document usage examples

### Technical Notes
```csharp
public class PunchEntityBuilder
{
    private PunchEntity _entity = new();

    public PunchEntityBuilder WithPunchIn(DateTime time)
    {
        _entity.PunchIn = time;
        return this;
    }

    public PunchEntityBuilder WithAuthId(string authId)
    {
        _entity.AuthId = authId;
        return this;
    }

    public PunchEntity Build() => _entity;
}
```

### Files to Create
- `tests/TimeApi.Tests/Builders/PunchEntityBuilder.cs`
- `tests/TimeApi.Tests/Builders/PunchRecordBuilder.cs`
- `tests/TimeApi.Tests/Fixtures/TestDataFactory.cs`

---

## US-008: Document Testing Standards and Guidelines

**As a** developer
**I want** clear testing guidelines and standards
**So that** all team members write consistent, quality tests

### Acceptance Criteria
- [ ] Create testing guidelines document
- [ ] Document naming conventions (Given_When_Then or MethodName_Scenario_ExpectedResult)
- [ ] Document AAA pattern (Arrange, Act, Assert)
- [ ] Provide examples of good vs bad tests
- [ ] Document when to use unit vs integration tests
- [ ] Document mocking best practices
- [ ] Include test-driven development (TDD) workflow

### Technical Notes
**Test Naming Convention:**
```csharp
[Fact]
public void InsertPunch_WhenPunchingIn_CreatesNewRecord()
{
    // Arrange - setup test data and mocks
    // Act - execute the method under test
    // Assert - verify the expected outcome
}
```

### Files to Create
- `docs/TESTING_GUIDELINES.md`
- `docs/examples/test-examples.md`

---

## Definition of Done (Epic 0)
- [ ] All test projects created and configured
- [ ] Existing code has 70%+ test coverage
- [ ] Tests run successfully in CI/CD pipeline
- [ ] Code coverage reporting configured
- [ ] Test data builders available for common scenarios
- [ ] Testing guidelines documented
- [ ] All tests passing
- [ ] Team can write new tests following established patterns
- [ ] Code reviewed and merged to main branch

---

## Notes
- **CRITICAL**: Complete this epic BEFORE starting feature development
- All new code in subsequent epics must include tests
- PRs should not be merged without corresponding tests
- Aim for TDD: Write tests first, then implement features
