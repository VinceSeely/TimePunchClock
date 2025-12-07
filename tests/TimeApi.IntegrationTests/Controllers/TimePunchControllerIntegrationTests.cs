using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using TimeClock.Client;

namespace TimeApi.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for TimePunchController endpoints.
/// Tests the full request/response cycle including database interactions.
/// </summary>
[TestFixture]
public class TimePunchControllerIntegrationTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GetPunchRecords_ReturnsOkStatus()
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
    public async Task GetPunchRecords_ReturnsEmptyArray_WhenNoPunches()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;

        // Act
        var response = await _client.GetAsync(
            $"/api/TimePunch?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}");
        var punches = await response.Content.ReadFromJsonAsync<PunchRecord[]>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        punches.Should().NotBeNull();
        punches.Should().BeEmpty();
    }

    [Test]
    public async Task GetLastPunch_ReturnsNotFound_WhenNoPunches()
    {
        // Act
        var response = await _client.GetAsync("/api/TimePunch/lastpunch");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "there are no punches in the database yet");
    }

    [Test]
    public async Task PostPunch_PunchIn_ReturnsOkStatus()
    {
        // Arrange
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.Regular
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/TimePunch", punchInfo);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task PostPunch_PunchInThenPunchOut_CreatesCompletePunchRecord()
    {
        // Arrange - Punch In
        var punchIn = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.TechLead
        };

        // Act - Punch In
        var punchInResponse = await _client.PostAsJsonAsync("/api/TimePunch", punchIn);
        punchInResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(100); // Small delay to ensure different timestamps

        // Arrange - Punch Out
        var punchOut = new PunchInfo
        {
            PunchType = PunchType.PunchOut,
            HourType = HourType.TechLead
        };

        // Act - Punch Out
        var punchOutResponse = await _client.PostAsJsonAsync("/api/TimePunch", punchOut);
        var lastPunch = await punchOutResponse.Content.ReadFromJsonAsync<PunchRecord>();

        // Assert
        punchOutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        lastPunch.Should().NotBeNull();
        lastPunch!.PunchIn.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        lastPunch.PunchOut.Should().NotBeNull();
        lastPunch.PunchOut.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        lastPunch.HourType.Should().Be(HourType.TechLead);
    }
}
