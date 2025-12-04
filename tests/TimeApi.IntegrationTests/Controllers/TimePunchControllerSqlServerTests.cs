using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using TimeClock.Client;

namespace TimeApi.IntegrationTests.Controllers;

/// <summary>
/// Integration tests using real SQL Server container.
/// These tests are slower but more realistic - use for testing:
/// - Database migrations
/// - SQL-specific features (constraints, indexes)
/// - Complex queries that might behave differently in-memory
/// </summary>
[TestFixture]
[Category("RequiresDocker")]
public class TimePunchControllerSqlServerTests
{
    private SqlServerWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new SqlServerWebApplicationFactory();
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client?.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Test]
    public async Task GetPunchRecords_WithRealSqlServer_ReturnsOkStatus()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;

        // Act
        var response = await _client.GetAsync(
            $"/api/TimePunch?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task PostPunch_WithRealSqlServer_CreatesPunchRecord()
    {
        // Arrange
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.Regular
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/TimePunch", punchInfo);
        var result = await response.Content.ReadFromJsonAsync<PunchRecord>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.PunchId.Should().NotBeEmpty();
        result.PunchIn.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task PostPunch_AutoClosesPreviousPunch_WithRealSqlServer()
    {
        // Arrange - Create first punch in
        var firstPunch = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.Regular
        };
        await _client.PostAsJsonAsync("/api/TimePunch", firstPunch);

        await Task.Delay(100);

        // Act - Create second punch in (should auto-close first)
        var secondPunch = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.TechLead
        };
        var response = await _client.PostAsJsonAsync("/api/TimePunch", secondPunch);

        // Assert - Verify second punch created successfully
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lastPunch = await response.Content.ReadFromJsonAsync<PunchRecord>();
        lastPunch.Should().NotBeNull();
        lastPunch!.HourType.Should().Be(HourType.TechLead);
        lastPunch.PunchOut.Should().BeNull("new punch should not be closed yet");
    }
}
