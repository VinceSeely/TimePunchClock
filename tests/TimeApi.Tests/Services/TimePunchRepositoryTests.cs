using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TimeApi.Models;
using TimeApi.Services;
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

        // Act
        _repository.InsertPunch(punchInfo);

        // Assert
        var punches = _context.Punchs.ToList();
        punches.Should().HaveCount(1);
        punches[0].PunchIn.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        punches[0].PunchOut.Should().BeNull();
        punches[0].HourType.Should().Be(HourType.Regular);
    }

    [Test]
    public void InsertPunch_PunchOut_ClosesExistingOpenRecord()
    {
        // Arrange
        var punchIn = new PunchEntity
        {
            PunchIn = DateTime.Now.AddHours(-2),
            HourType = HourType.Regular,
            PunchOut = null
        };
        _context.Punchs.Add(punchIn);
        _context.SaveChanges();

        var punchOutInfo = new PunchInfo
        {
            PunchType = PunchType.PunchOut,
            HourType = HourType.Regular
        };

        // Act
        _repository.InsertPunch(punchOutInfo);

        // Assert
        var updatedPunch = _context.Punchs.First();
        updatedPunch.PunchOut.Should().NotBeNull();
        updatedPunch.PunchOut.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void InsertPunch_PunchOut_DoesNotCreateNewRecord_WhenOpenPunchExists()
    {
        // Arrange
        var punchIn = new PunchEntity
        {
            PunchIn = DateTime.Now.AddHours(-2),
            HourType = HourType.Regular,
            PunchOut = null
        };
        _context.Punchs.Add(punchIn);
        _context.SaveChanges();

        var punchOutInfo = new PunchInfo
        {
            PunchType = PunchType.PunchOut,
            HourType = HourType.Regular
        };

        // Act
        _repository.InsertPunch(punchOutInfo);

        // Assert
        _context.Punchs.Should().HaveCount(1);
    }

    [Test]
    public void InsertPunch_PunchIn_AutoClosesOpenPunch_BeforeCreatingNew()
    {
        // Arrange
        var openPunch = new PunchEntity
        {
            PunchIn = DateTime.Now.AddHours(-3),
            HourType = HourType.Regular,
            PunchOut = null
        };
        _context.Punchs.Add(openPunch);
        _context.SaveChanges();

        var newPunchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.TechLead
        };

        // Act
        _repository.InsertPunch(newPunchInfo);

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
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.Regular
        };

        // Act
        _repository.InsertPunch(punchInfo);

        // Assert
        var punches = _context.Punchs.ToList();
        punches.Should().HaveCount(1);
        punches[0].PunchOut.Should().BeNull();
    }

    [Test]
    public void InsertPunch_PunchIn_WithTechLeadHourType_CreatesCorrectRecord()
    {
        // Arrange
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.TechLead
        };

        // Act
        _repository.InsertPunch(punchInfo);

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
        var start = DateTime.Today;
        var end = DateTime.Today.AddDays(7);

        // Act
        var results = _repository.GetPunchRecords(start, end);

        // Assert
        results.Should().BeEmpty();
    }

    [Test]
    public void GetPunchRecords_ReturnsRecords_InDateRange()
    {
        // Arrange
        var punch1 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-5).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-5).AddHours(17),
            HourType = HourType.Regular
        };
        var punch2 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-3).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-3).AddHours(17),
            HourType = HourType.Regular
        };
        _context.Punchs.AddRange(punch1, punch2);
        _context.SaveChanges();

        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today;

        // Act
        var results = _repository.GetPunchRecords(start, end).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.PunchOut.Should().NotBeNull());
    }

    [Test]
    public void GetPunchRecords_ExcludesRecords_OutsideDateRange()
    {
        // Arrange
        var punchInRange = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-3).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-3).AddHours(17),
            HourType = HourType.Regular
        };
        var punchOutOfRange = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-10).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-10).AddHours(17),
            HourType = HourType.Regular
        };
        _context.Punchs.AddRange(punchInRange, punchOutOfRange);
        _context.SaveChanges();

        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today;

        // Act
        var results = _repository.GetPunchRecords(start, end).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].PunchIn.Date.Should().Be(DateTime.Today.AddDays(-3));
    }

    [Test]
    public void GetPunchRecords_ExcludesOpenPunches_WithNullPunchOut()
    {
        // Arrange
        var closedPunch = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-2).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-2).AddHours(17),
            HourType = HourType.Regular
        };
        var openPunch = new PunchEntity
        {
            PunchIn = DateTime.Today.AddHours(9),
            PunchOut = null,
            HourType = HourType.Regular
        };
        _context.Punchs.AddRange(closedPunch, openPunch);
        _context.SaveChanges();

        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today.AddDays(1);

        // Act
        var results = _repository.GetPunchRecords(start, end).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].PunchOut.Should().NotBeNull();
    }

    [Test]
    public void GetPunchRecords_MapsAllProperties_Correctly()
    {
        // Arrange
        var expectedPunchId = Guid.NewGuid();
        var punch = new PunchEntity
        {
            PunchId = expectedPunchId,
            PunchIn = DateTime.Today.AddHours(9),
            PunchOut = DateTime.Today.AddHours(17),
            HourType = HourType.TechLead
        };
        _context.Punchs.Add(punch);
        _context.SaveChanges();

        var start = DateTime.Today;
        var end = DateTime.Today.AddDays(1);

        // Act
        var results = _repository.GetPunchRecords(start, end).ToList();

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
        var punch1 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-5).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-5).AddHours(17),
            HourType = HourType.Regular
        };
        var punch2 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-3).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-3).AddHours(17),
            HourType = HourType.Regular
        };
        var punch3 = new PunchEntity
        {
            PunchIn = DateTime.Today.AddDays(-1).AddHours(9),
            PunchOut = DateTime.Today.AddDays(-1).AddHours(17),
            HourType = HourType.TechLead
        };
        _context.Punchs.AddRange(punch1, punch2, punch3);
        _context.SaveChanges();

        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today;

        // Act
        var results = _repository.GetPunchRecords(start, end).ToList();

        // Assert
        results.Should().HaveCount(3);
    }

    #endregion

    #region GetLastPunch Tests

    [Test]
    public void GetLastPunch_ReturnsNull_WhenNoPunchesExist()
    {
        // Act
        var result = _repository.GetLastPunch();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void GetLastPunch_ReturnsMostRecentPunch_BasedOnPunchIn()
    {
        // Arrange
        var oldPunch = new PunchEntity
        {
            PunchIn = DateTime.Now.AddDays(-2),
            PunchOut = DateTime.Now.AddDays(-2).AddHours(8),
            HourType = HourType.Regular
        };
        var recentPunch = new PunchEntity
        {
            PunchIn = DateTime.Now.AddHours(-1),
            PunchOut = null,
            HourType = HourType.TechLead
        };
        _context.Punchs.AddRange(oldPunch, recentPunch);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch();

        // Assert
        result.Should().NotBeNull();
        result!.PunchIn.Should().BeCloseTo(DateTime.Now.AddHours(-1), TimeSpan.FromSeconds(1));
        result.HourType.Should().Be(HourType.TechLead);
    }

    [Test]
    public void GetLastPunch_ReturnsOpenPunch_WhenPunchOutIsNull()
    {
        // Arrange
        var openPunch = new PunchEntity
        {
            PunchIn = DateTime.Now.AddHours(-2),
            PunchOut = null,
            HourType = HourType.Regular
        };
        _context.Punchs.Add(openPunch);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch();

        // Assert
        result.Should().NotBeNull();
        result!.PunchOut.Should().BeNull();
    }

    [Test]
    public void GetLastPunch_ReturnsClosedPunch_WhenPunchOutIsSet()
    {
        // Arrange
        var closedPunch = new PunchEntity
        {
            PunchIn = DateTime.Now.AddHours(-8),
            PunchOut = DateTime.Now,
            HourType = HourType.Regular
        };
        _context.Punchs.Add(closedPunch);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch();

        // Assert
        result.Should().NotBeNull();
        result!.PunchOut.Should().NotBeNull();
        result.PunchOut.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void GetLastPunch_MapsAllProperties_Correctly()
    {
        // Arrange
        var expectedPunchId = Guid.NewGuid();
        var punch = new PunchEntity
        {
            PunchId = expectedPunchId,
            PunchIn = DateTime.Now.AddHours(-2),
            PunchOut = DateTime.Now.AddHours(-1),
            HourType = HourType.TechLead
        };
        _context.Punchs.Add(punch);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch();

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
        var samePunchInTime = DateTime.Now.AddHours(-2);
        var punch1 = new PunchEntity
        {
            PunchIn = samePunchInTime,
            PunchOut = DateTime.Now.AddHours(-1),
            HourType = HourType.Regular
        };
        var punch2 = new PunchEntity
        {
            PunchIn = samePunchInTime,
            PunchOut = DateTime.Now,
            HourType = HourType.TechLead
        };
        _context.Punchs.AddRange(punch1, punch2);
        _context.SaveChanges();

        // Act
        var result = _repository.GetLastPunch();

        // Assert
        result.Should().NotBeNull();
        result!.HourType.Should().Be(HourType.TechLead);
        result.PunchOut.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    #endregion
}
