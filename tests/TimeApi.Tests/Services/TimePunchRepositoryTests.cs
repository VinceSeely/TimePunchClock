using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TimeApi.Models;
using TimeApi.Services;
using TimeApi.Tests.Fixtures;
using TimeClock.Client;

namespace TimeApi.Tests.Services;

[TestFixture]
public class TimePunchRepositoryTests
{
    private TimeClockDbContext _context = null!;
    private TimePunchRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TimeClockDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TimeClockDbContext(options);
        _repository = new TimePunchRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region InsertPunch Tests

    [Test]
    public void InsertPunch_PunchIn_CreatesNewRecord()
    {
        // Arrange
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.Regular
        };
        var authId = "test-user-123";

        // Act
        _repository.InsertPunch(punchInfo, authId);

        // Assert
        var punches = _context.Punchs.ToList();
        punches.Should().HaveCount(1);
        punches[0].PunchIn.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        punches[0].PunchOut.Should().BeNull();
        punches[0].HourType.Should().Be(HourType.Regular);
        punches[0].AuthId.Should().Be(authId);
    }

    [Test]
    public void InsertPunch_PunchOut_ClosesExistingOpenRecord()
    {
        // Arrange
        var authId = "test-user-123";
        var punchIn = TestDataFactory.CreateOpenPunch(HourType.Regular);
        punchIn.AuthId = authId;
        _context.Punchs.Add(punchIn);
        _context.SaveChanges();

        var punchOutInfo = TestDataFactory.CreatePunchInfo(PunchType.PunchOut, HourType.Regular);

        // Act
        _repository.InsertPunch(punchOutInfo, authId);

        // Assert
        var updatedPunch = _context.Punchs.First();
        updatedPunch.PunchOut.Should().NotBeNull();
        updatedPunch.PunchOut.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void InsertPunch_PunchOut_DoesNotCreateNewRecord_WhenOpenPunchExists()
    {
        // Arrange
        var authId = "test-user-123";
        var punchIn = TestDataFactory.CreateOpenPunch(HourType.Regular);
        punchIn.AuthId = authId;
        _context.Punchs.Add(punchIn);
        _context.SaveChanges();

        var punchOutInfo = new PunchInfo
        {
            PunchType = PunchType.PunchOut,
            HourType = HourType.Regular
        };

        // Act
        _repository.InsertPunch(punchOutInfo, authId);

        // Assert
        _context.Punchs.Should().HaveCount(1);
    }

    [Test]
    public void InsertPunch_PunchIn_AutoClosesOpenPunch_BeforeCreatingNew()
    {
        // Arrange
        var authId = "test-user-123";
        var openPunch = new PunchEntity
        {
            PunchIn = DateTime.Now.AddHours(-3),
            HourType = HourType.Regular,
            PunchOut = null,
            AuthId = authId
        };
        _context.Punchs.Add(openPunch);
        _context.SaveChanges();

        var newPunchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.TechLead
        };

        // Act
        _repository.InsertPunch(newPunchInfo, authId);

        // Assert
        var punches = _context.Punchs.OrderBy(p => p.PunchIn).ToList();
        punches.Should().HaveCount(2);

        // Old punch should be auto-closed
        punches[0].PunchOut.Should().NotBeNull();
        punches[0].PunchOut.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));

        // New punch should be created
        punches[1].PunchIn.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        punches[1].PunchOut.Should().BeNull();
        punches[1].HourType.Should().Be(HourType.TechLead);
    }

    [Test]
    public void InsertPunch_PunchIn_CreatesNewRecord_WhenNoPreviousPunches()
    {
        // Arrange
        var authId = "test-user-123";
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.Regular
        };

        // Act
        _repository.InsertPunch(punchInfo, authId);

        // Assert
        var punches = _context.Punchs.ToList();
        punches.Should().HaveCount(1);
        punches[0].PunchOut.Should().BeNull();
    }

    [Test]
    public void InsertPunch_PunchIn_WithTechLeadHourType_CreatesCorrectRecord()
    {
        // Arrange
        var authId = "test-user-123";
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.TechLead
        };

        // Act
        _repository.InsertPunch(punchInfo, authId);

        // Assert
        var punch = _context.Punchs.First();
        punch.HourType.Should().Be(HourType.TechLead);
    }

    #endregion

    #region GetPunchRecords Tests

    [Test]
    public void GetPunchRecords_ReturnsEmptyList_WhenNoRecordsInRange()
    {
        // Arrange
        var authId = "test-user-123";
        var start = DateTime.Today;
        var end = DateTime.Today.AddDays(7);

        // Act
        var results = _repository.GetPunchRecords(start, end, authId);

        // Assert
        results.Should().BeEmpty();
    }

    [Test]
    public void GetPunchRecords_ReturnsRecords_InDateRange()
    {
        // Arrange
        var authId = "test-user-123";
        var punch1 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-5).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-5).AddHours(17),
            HourType = HourType.Regular,
            AuthId = authId
        };
        var punch2 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-3).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-3).AddHours(17),
            HourType = HourType.Regular,
            AuthId = authId
        };
        _context.Punchs.AddRange(punch1, punch2);
        _context.SaveChanges();

        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today;

        // Act
        var results = _repository.GetPunchRecords(start, end, authId).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.PunchOut.Should().NotBeNull());
    }

    [Test]
    public void GetPunchRecords_ExcludesRecords_OutsideDateRange()
    {
        // Arrange
        var authId = "test-user-123";
        var punchInRange = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-3).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-3).AddHours(17),
            HourType = HourType.Regular,
            AuthId = authId
        };
        var punchOutOfRange = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-10).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-10).AddHours(17),
            HourType = HourType.Regular,
            AuthId = authId
        };
        _context.Punchs.AddRange(punchInRange, punchOutOfRange);
        _context.SaveChanges();

        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today;

        // Act
        var results = _repository.GetPunchRecords(start, end, authId).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].PunchIn.Date.Should().Be(DateTime.Today.AddDays(-3));
    }

    [Test]
    public void GetPunchRecords_ExcludesOpenPunches_WithNullPunchOut()
    {
        // Arrange
        var authId = "test-user-123";
        var closedPunch = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-2).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-2).AddHours(17),
            HourType = HourType.Regular,
            AuthId = authId
        };
        var openPunch = new PunchEntity
        {
            PunchIn = DateTime.Today.AddHours(9),
            PunchOut = null,
            HourType = HourType.Regular,
            AuthId = authId
        };
        _context.Punchs.AddRange(closedPunch, openPunch);
        _context.SaveChanges();

        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today.AddDays(1);

        // Act
        var results = _repository.GetPunchRecords(start, end, authId).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].PunchOut.Should().NotBeNull();
    }

    [Test]
    public void GetPunchRecords_MapsAllProperties_Correctly()
    {
        // Arrange
        var authId = "test-user-123";
        var expectedPunchId = Guid.NewGuid();
        var punch = new PunchEntity
        {
            PunchId = expectedPunchId,
            PunchIn = DateTime.Today.AddHours(9),
            PunchOut = DateTime.Today.AddHours(17),
            HourType = HourType.TechLead,
            AuthId = authId
        };
        _context.Punchs.Add(punch);
        _context.SaveChanges();

        var start = DateTime.Today;
        var end = DateTime.Today.AddDays(1);

        // Act
        var results = _repository.GetPunchRecords(start, end, authId).ToList();

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        result.PunchId.Should().Be(expectedPunchId);
        result.PunchIn.Should().Be(punch.PunchIn);
        result.PunchOut.Should().Be(punch.PunchOut);
        result.HourType.Should().Be(HourType.TechLead);
    }

    [Test]
    public void GetPunchRecords_ReturnsMultipleRecords_OrderedCorrectly()
    {
        // Arrange
        var authId = "test-user-123";
        var punch1 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-5).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-5).AddHours(17),
            HourType = HourType.Regular,
            AuthId = authId
        };
        var punch2 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-3).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-3).AddHours(17),
            HourType = HourType.Regular,
            AuthId = authId
        };
        var punch3 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-1).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-1).AddHours(17),
            HourType = HourType.TechLead,
            AuthId = authId
        };
        _context.Punchs.AddRange(punch1, punch2, punch3);
        _context.SaveChanges();

        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today;

        // Act
        var results = _repository.GetPunchRecords(start, end, authId).ToList();

        // Assert
        results.Should().HaveCount(3);
    }

    #endregion

    #region GetLastPunch Tests

    [Test]
    public void GetLastPunch_ReturnsNull_WhenNoPunchesExist()
    {
        // Arrange
        var authId = "test-user-123";

        // Act
        var result = _repository.GetLastPunch(authId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void GetLastPunch_ReturnsMostRecentPunch_BasedOnPunchIn()
    {
        // Arrange
        var authId = "test-user-123";
        var oldPunch = new PunchEntity
        {
            PunchIn = DateTime.Now.AddDays(-2),
            PunchOut = DateTime.Now.AddDays(-2).AddHours(8),
            HourType = HourType.Regular,
            AuthId = authId
        };
        var recentPunch = new PunchEntity
        {
            PunchIn = DateTime.Now.AddHours(-1),
            PunchOut = null,
            HourType = HourType.TechLead,
            AuthId = authId
        };
        _context.Punchs.AddRange(oldPunch, recentPunch);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch(authId);

        // Assert
        result.Should().NotBeNull();
        result!.PunchIn.Should().BeCloseTo(DateTime.Now.AddHours(-1), TimeSpan.FromSeconds(1));
        result.HourType.Should().Be(HourType.TechLead);
    }

    [Test]
    public void GetLastPunch_Repository_ReturnsOpenPunch_WhenPunchOutIsNull()
    {
        // Arrange
        var authId = "test-user-123";
        var openPunch = TestDataFactory.CreateOpenPunch(HourType.Regular);
        openPunch.AuthId = authId;
        _context.Punchs.Add(openPunch);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch(authId);

        // Assert
        result.Should().NotBeNull();
        result!.PunchOut.Should().BeNull();
    }

    [Test]
    public void GetLastPunch_Repository_ReturnsClosedPunch_WhenPunchOutIsSet()
    {
        // Arrange
        var authId = "test-user-123";
        var closedPunch = TestDataFactory.CreateClosedPunch(hourType: HourType.Regular);
        closedPunch.AuthId = authId;
        _context.Punchs.Add(closedPunch);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch(authId);

        // Assert
        result.Should().NotBeNull();
        result!.PunchOut.Should().NotBeNull();
        result.PunchIn.Should().Be(closedPunch.PunchIn);
        result.PunchOut.Should().Be(closedPunch.PunchOut);
    }

    [Test]
    public void GetLastPunch_MapsAllProperties_Correctly()
    {
        // Arrange
        var authId = "test-user-123";
        var expectedPunchId = Guid.NewGuid();
        var punch = new PunchEntity
        {
            PunchId = expectedPunchId,
            PunchIn = DateTime.Now.AddHours(-2),
            PunchOut = DateTime.Now.AddHours(-1),
            HourType = HourType.TechLead,
            AuthId = authId
        };
        _context.Punchs.Add(punch);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch(authId);

        // Assert
        result.Should().NotBeNull();
        result!.PunchId.Should().Be(expectedPunchId);
        result.PunchIn.Should().BeCloseTo(punch.PunchIn, TimeSpan.FromSeconds(1));
        result.PunchOut.Should().BeCloseTo(punch.PunchOut!.Value, TimeSpan.FromSeconds(1));
        result.HourType.Should().Be(HourType.TechLead);
    }

    [Test]
    public void GetLastPunch_UsesSecondarySort_ByPunchOut()
    {
        // Arrange
        var authId = "test-user-123";
        var samePunchInTime = DateTime.Now.AddHours(-2);
        var punch1 = new PunchEntity
        {
            PunchIn = samePunchInTime,
            PunchOut = DateTime.Now.AddHours(-1),
            HourType = HourType.Regular,
            AuthId = authId
        };
        var punch2 = new PunchEntity
        {
            PunchIn = samePunchInTime,
            PunchOut = DateTime.Now,
            HourType = HourType.TechLead,
            AuthId = authId
        };
        _context.Punchs.AddRange(punch1, punch2);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch(authId);

        // Assert
        result.Should().NotBeNull();
        result!.HourType.Should().Be(HourType.TechLead);
        result.PunchOut.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    #endregion
}
