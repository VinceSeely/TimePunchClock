# Testing Guidelines

## Overview

This document outlines testing standards and best practices for the TimeClock project using **NUnit**. All contributors should follow these guidelines to maintain code quality and consistency.

## Test Coverage Goals

- **Minimum Coverage**: 70% for all code
- **Target Coverage**: 80%+ for business logic (repositories, services)
- **Controllers**: 80%+ coverage
- **Models/DTOs**: Not required (simple POCOs)

## Test Organization

### Project Structure

```
tests/
├── TimeApi.Tests/              # Unit tests
│   ├── Api/                    # Controller tests
│   ├── Services/               # Repository/service tests
│   ├── Builders/               # Test data builders (optional)
│   └── Fixtures/               # Test data factories
├── TimeApi.IntegrationTests/   # Integration tests
│   └── Controllers/            # Full API integration tests
└── TimeClockUI.Tests/          # Blazor component tests
    └── Pages/                  # Page component tests
```

## NUnit Basics

### Test Class Structure
```csharp
[TestFixture]
public class TimePunchRepositoryTests
{
    private TimeClockDbContext _context = null!;
    private TimePunchRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        // Runs before EACH test
        _context = CreateInMemoryContext();
        _repository = new TimePunchRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        // Runs after EACH test
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Runs ONCE before all tests in this class
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Runs ONCE after all tests in this class
    }

    [Test]
    public void MyTest()
    {
        // Individual test
    }
}
```

### Common NUnit Attributes

```csharp
[Test]                          // Marks a test method
[TestFixture]                   // Marks a test class
[SetUp]                         // Runs before each test
[TearDown]                      // Runs after each test
[OneTimeSetUp]                  // Runs once before all tests
[OneTimeTearDown]               // Runs once after all tests
[Ignore("Reason")]              // Skip this test
[Category("Integration")]       // Categorize tests
[TestCase(1, 2, 3)]            // Parameterized test
[Values(1, 2, 3)]              // Test with multiple values
```

## Test Naming Conventions

Use one of these naming patterns:

### Pattern 1: MethodName_Scenario_ExpectedResult
```csharp
[Test]
public void InsertPunch_WhenPunchingIn_CreatesNewRecord()
{
    // Test implementation
}

[Test]
public void GetPunchRecords_WithEmptyDatabase_ReturnsEmptyList()
{
    // Test implementation
}
```

### Pattern 2: Given_When_Then
```csharp
[Test]
public void GivenOpenPunch_WhenPunchingIn_ThenAutoClosesAndCreatesNew()
{
    // Test implementation
}
```

**Choose one pattern per test class for consistency.**

## Test Structure: AAA Pattern

All tests should follow the **Arrange-Act-Assert (AAA)** pattern:

```csharp
[Test]
public void InsertPunch_PunchIn_CreatesNewRecord()
{
    // Arrange - setup test data and mocks
    var punchInfo = new PunchInfo
    {
        PunchType = PunchType.PunchIn,
        HourType = HourType.Normal
    };

    // Act - execute the method under test
    _repository.InsertPunch(punchInfo);

    // Assert - verify the expected outcome
    var punches = _context.Punchs.ToList();
    punches.Should().HaveCount(1);
    punches[0].HourType.Should().Be(HourType.Normal);
}
```

## Test Data Creation

You have **three options** for creating test data. Choose based on what's most readable for your test:

### Option 1: Direct Constructor (Simplest)
```csharp
var entity = new PunchEntity
{
    PunchIn = DateTime.Now,
    HourType = HourType.Normal,
    AuthId = "user-123"
};
```

### Option 2: Builder Pattern (For Complex Objects)
```csharp
var entity = new PunchEntityBuilder()
    .WithPunchIn(DateTime.Today.AddHours(9))
    .WithHourType(HourType.Normal)
    .WithAuthId("user-123")
    .AsClosedPunch()
    .Build();

// Or use implicit conversion (no Build() needed):
PunchEntity entity = new PunchEntityBuilder()
    .WithAuthId("user-123");
```

### Option 3: Test Data Factory (For Common Scenarios)
```csharp
// Quick common scenarios
var openPunch = TestDataFactory.CreateOpenPunch(HourType.Normal, "user-123");
var closedPunch = TestDataFactory.CreateClosedPunch(authId: "user-123", workDescription: "Fixed bug");
var weekOfPunches = TestDataFactory.CreateWeekOfPunches(DateTime.Today, "user-123");
```

**Recommendation**: Use direct construction for simple cases, builders for complex scenarios, and factory methods for common patterns.

## Unit Tests vs Integration Tests

### Unit Tests (`TimeApi.Tests`)

- Test **individual methods** in isolation
- Use **mocking** for dependencies
- Use **in-memory database** (fast, no external dependencies)
- Run in **milliseconds**
- Should be **independent** and **idempotent**

Example:
```csharp
[Test]
public void GetLastPunch_ReturnsNull_WhenNoPunchesExist()
{
    // Act
    var result = _repository.GetLastPunch();

    // Assert
    result.Should().BeNull();
}
```

### Integration Tests (`TimeApi.IntegrationTests`)

- Test **complete workflows** end-to-end
- Test **real database interactions** (SQL Server via Testcontainers or in-memory)
- Test **HTTP endpoints** via WebApplicationFactory
- Run in **seconds**
- Validate **API contracts** and **serialization**

Example:
```csharp
[Test]
public async Task GetPunchRecords_ReturnsOkStatus()
{
    // Arrange
    var client = _factory.CreateClient();
    var start = DateTime.Today;
    var end = DateTime.Today;

    // Act
    var response = await client.GetAsync($"/api/TimePunch?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Parameterized Tests

NUnit supports parameterized tests with `[TestCase]`:

```csharp
[TestCase(HourType.Normal)]
[TestCase(HourType.Overtime)]
[TestCase(HourType.TechLead)]
public void InsertPunch_WithDifferentHourTypes_CreatesCorrectRecord(HourType hourType)
{
    // Arrange
    var punchInfo = new PunchInfo
    {
        PunchType = PunchType.PunchIn,
        HourType = hourType
    };

    // Act
    _repository.InsertPunch(punchInfo);

    // Assert
    var punch = _context.Punchs.First();
    punch.HourType.Should().Be(hourType);
}
```

Or use `[Values]` for single parameters:
```csharp
[Test]
public void InsertPunch_WithHourType_CreatesRecord([Values] HourType hourType)
{
    // Test implementation
}
```

## Mocking Best Practices

### What to Mock
- External dependencies (databases, APIs, file systems)
- Services with complex logic
- Time-dependent operations (use `IClock` abstraction)

### What NOT to Mock
- Simple POCOs/DTOs
- Value objects
- The class under test
- Entity Framework queries (use in-memory database instead)

### Mocking Example
```csharp
[Test]
public void GetPunchRecords_CallsRepository_WithCorrectParameters()
{
    // Arrange
    var mockRepo = new Mock<ITimePunchRepository>();
    var controller = new TimePunchController(mockRepo.Object);
    var start = new DateTime(2025, 1, 1);
    var end = new DateTime(2025, 1, 31);

    // Act
    controller.GetPunchRecords(start, end);

    // Assert
    mockRepo.Verify(r => r.GetPunchRecords(start, end), Times.Once);
}
```

## Assertions with FluentAssertions

Use **FluentAssertions** for readable, expressive assertions:

```csharp
// ❌ Bad - NUnit classic assertions
Assert.IsNotNull(result);
Assert.AreEqual(5, result.Count());
Assert.IsTrue(result.Any(r => r.HourType == HourType.Normal));

// ✅ Good - FluentAssertions (clear and readable)
result.Should().NotBeNull();
result.Should().HaveCount(5);
result.Should().Contain(r => r.HourType == HourType.Normal);
```

### Common FluentAssertions Patterns
```csharp
// Collections
result.Should().BeEmpty();
result.Should().HaveCount(3);
result.Should().Contain(item);
result.Should().OnlyContain(r => r.AuthId == "user-123");

// Objects
entity.Should().NotBeNull();
entity.PunchOut.Should().BeNull();
entity.HourType.Should().Be(HourType.Normal);

// Dates
punch.PunchIn.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
punch.PunchIn.Should().BeBefore(punch.PunchOut.Value);

// Exceptions
Action act = () => repository.InsertPunch(null!);
act.Should().Throw<ArgumentNullException>();
```

## When to Write Tests

### Test-Driven Development (TDD) - Preferred
1. **Write the test first** (it will fail - Red)
2. **Write minimal code** to make it pass (Green)
3. **Refactor** for clarity (Refactor)
4. Repeat

### Test-After Development - Acceptable
1. Write the implementation
2. **Immediately** write tests before moving on
3. Ensure **80%+ coverage** for business logic

### Never Acceptable
- ❌ Writing code without tests
- ❌ Committing untested code
- ❌ Merging PRs with failing tests
- ❌ Reducing test coverage

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test tests/TimeApi.Tests/TimeApi.Tests.csproj
```

### Run Tests by Category
```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category!=Integration"  # Skip integration tests
```

### Run Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Tests in Watch Mode (TDD)
```bash
dotnet watch test --project tests/TimeApi.Tests/TimeApi.Tests.csproj
```

## Code Coverage

### Generate Coverage Report
```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Install report generator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html

# View report
start coverage-report/index.html  # Windows
open coverage-report/index.html   # Mac
```

## Common Testing Scenarios

### Testing Date Ranges
```csharp
[Test]
public void GetPunchRecords_ReturnsRecords_InDateRange()
{
    // Arrange
    var jan1 = new DateTime(2025, 1, 1, 9, 0, 0);

    _context.Punchs.AddRange(
        TestDataFactory.CreateClosedPunch(jan1),
        TestDataFactory.CreateClosedPunch(jan1.AddDays(5)),
        TestDataFactory.CreateClosedPunch(jan1.AddDays(40)) // Outside range
    );
    _context.SaveChanges();

    // Act
    var result = _repository.GetPunchRecords(
        new DateTime(2025, 1, 1),
        new DateTime(2025, 1, 31)
    );

    // Assert
    result.Should().HaveCount(2); // Only Jan records
}
```

### Testing Async Methods
```csharp
[Test]
public async Task PostPunch_ReturnsOkStatus()
{
    // Arrange
    var client = _factory.CreateClient();
    var punch = new PunchInfo { PunchType = PunchType.PunchIn, HourType = HourType.Normal };
    var content = JsonContent.Create(punch);

    // Act
    var response = await client.PostAsync("/api/TimePunch", content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### Testing Null/Edge Cases
```csharp
[Test]
public void GetLastPunch_ReturnsNull_WhenNoPunchesExist()
{
    var result = _repository.GetLastPunch();

    result.Should().BeNull();
}

[Test]
public void InsertPunch_WithNullPunchOut_CreatesOpenPunch()
{
    // Test that open punches work correctly
}
```

### Testing Exceptions
```csharp
[Test]
public void InsertPunch_WithNullPunchInfo_ThrowsException()
{
    // Act
    Action act = () => _repository.InsertPunch(null!);

    // Assert
    act.Should().Throw<ArgumentNullException>();
}
```

## CI/CD Integration

Tests run automatically on:
- Every push to branches
- Every pull request
- Before deployment

**Pull requests cannot be merged if:**
- ❌ Any tests fail
- ❌ Code coverage drops below 70%
- ❌ Build fails

## Troubleshooting

### Tests are Slow
- Use in-memory database instead of SQL Server for unit tests
- Run integration tests separately: `dotnet test tests/TimeApi.IntegrationTests`
- Use `[Category]` to categorize fast vs slow tests

### Flaky Tests (Random Failures)
- Avoid `DateTime.Now` - use fixed dates in tests
- Don't depend on test execution order
- Use unique database names for in-memory contexts: `Guid.NewGuid().ToString()`

### Docker Required for Integration Tests
- SQL Server integration tests need Docker running
- Use in-memory tests if Docker unavailable
- Skip integration tests: `dotnet test --filter "Category!=Integration"`

## Resources

- **NUnit Documentation**: https://docs.nunit.org/
- **FluentAssertions Documentation**: https://fluentassertions.com/
- **Moq Documentation**: https://github.com/moq/moq4
- **bUnit (Blazor Testing)**: https://bunit.dev/
- **Test Coverage**: Using Coverlet

## Examples

See these files for complete examples:
- `tests/TimeApi.Tests/Services/TimePunchRepositoryTests.cs` - Repository unit tests
- `tests/TimeApi.Tests/Api/TimePunchControllerTests.cs` - Controller unit tests
- `tests/TimeApi.IntegrationTests/Controllers/TimePunchControllerIntegrationTests.cs` - Integration tests
- `tests/TimeApi.Tests/Builders/` - Test data builders
- `tests/TimeApi.Tests/Fixtures/TestDataFactory.cs` - Test data factory patterns
