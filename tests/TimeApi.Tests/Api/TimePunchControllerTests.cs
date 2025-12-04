using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TimeApi.Api;
using TimeApi.Services;
using TimeClock.Client;

namespace TimeApi.Tests.Api;

[TestFixture]
public class TimePunchControllerTests
{
    private Mock<ITimePunchRepository> _mockRepository = null!;
    private TimePunchController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<ITimePunchRepository>(MockBehavior.Strict);
        _controller = new TimePunchController(_mockRepository.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _mockRepository.VerifyAll();
    }

    #region GetHours Tests

    [Test]
    public void GetHours_ReturnsOkWithRecords_WhenRecordsExist()
    {
        // Arrange
        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today;
        var expectedRecords = new List<PunchRecord>
        {
            new PunchRecord
            {
                PunchId = Guid.NewGuid(),
                PunchIn = DateTime.Today.AddDays(-2).AddHours(9),
                PunchOut = DateTime.Today.AddDays(-2).AddHours(17),
                HourType = HourType.Regular
            },
            new PunchRecord
            {
                PunchId = Guid.NewGuid(),
                PunchIn = DateTime.Today.AddDays(-1).AddHours(9),
                PunchOut = DateTime.Today.AddDays(-1).AddHours(17),
                HourType = HourType.TechLead
            }
        };

        _mockRepository
            .Setup(r => r.GetPunchRecords(start, end))
            .Returns(expectedRecords);

        // Act
        var result = _controller.GetHours(start, end);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(expectedRecords);
    }

    [Test]
    public void GetHours_ReturnsOkWithEmptyArray_WhenNoRecordsExist()
    {
        // Arrange
        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today;

        _mockRepository
            .Setup(r => r.GetPunchRecords(start, end))
            .Returns(new List<PunchRecord>());

        // Act
        var result = _controller.GetHours(start, end);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(Array.Empty<PunchRecord>());
    }

    [Test]
    public void GetHours_ReturnsOkWithEmptyArray_WhenRecordsAreNull()
    {
        // Arrange
        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today;

        _mockRepository
            .Setup(r => r.GetPunchRecords(start, end))
            .Returns((IEnumerable<PunchRecord>)null!);

        // Act
        var result = _controller.GetHours(start, end);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(Array.Empty<PunchRecord>());
    }

    [Test]
    public void GetHours_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 1, 31);

        _mockRepository
            .Setup(r => r.GetPunchRecords(start, end))
            .Returns(new List<PunchRecord>());

        // Act
        _controller.GetHours(start, end);

        // Assert - verified by Mock.VerifyAll() in TearDown
    }

    #endregion

    #region PunchHours Tests

    [Test]
    public void PunchHours_ReturnsOkWithLastPunch_AfterInserting()
    {
        // Arrange
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.Regular
        };

        var expectedLastPunch = new PunchRecord
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Now,
            PunchOut = null,
            HourType = HourType.Regular
        };

        _mockRepository
            .Setup(r => r.InsertPunch(punchInfo))
            .Verifiable();

        _mockRepository
            .Setup(r => r.GetLastPunch())
            .Returns(expectedLastPunch);

        // Act
        var result = _controller.PunchHours(punchInfo);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(expectedLastPunch);
    }

    [Test]
    public void PunchHours_CallsInsertPunchBeforeGetLastPunch()
    {
        // Arrange
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchOut,
            HourType = HourType.TechLead
        };

        var sequence = new MockSequence();

        _mockRepository
            .InSequence(sequence)
            .Setup(r => r.InsertPunch(punchInfo))
            .Verifiable();

        _mockRepository
            .InSequence(sequence)
            .Setup(r => r.GetLastPunch())
            .Returns(new PunchRecord
            {
                PunchId = Guid.NewGuid(),
                PunchIn = DateTime.Now.AddHours(-8),
                PunchOut = DateTime.Now,
                HourType = HourType.TechLead
            });

        // Act
        _controller.PunchHours(punchInfo);

        // Assert - verified by MockSequence and Mock.VerifyAll() in TearDown
    }

    [Test]
    public void PunchHours_WithPunchIn_ReturnsCorrectRecord()
    {
        // Arrange
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchIn,
            HourType = HourType.Regular
        };

        var expectedRecord = new PunchRecord
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Now,
            PunchOut = null,
            HourType = HourType.Regular
        };

        _mockRepository.Setup(r => r.InsertPunch(punchInfo)).Verifiable();
        _mockRepository.Setup(r => r.GetLastPunch()).Returns(expectedRecord);

        // Act
        var result = _controller.PunchHours(punchInfo);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRecord = okResult.Value.Should().BeAssignableTo<PunchRecord>().Subject;
        returnedRecord.PunchOut.Should().BeNull();
        returnedRecord.HourType.Should().Be(HourType.Regular);
    }

    [Test]
    public void PunchHours_WithPunchOut_ReturnsCorrectRecord()
    {
        // Arrange
        var punchInfo = new PunchInfo
        {
            PunchType = PunchType.PunchOut,
            HourType = HourType.TechLead
        };

        var expectedRecord = new PunchRecord
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Now.AddHours(-8),
            PunchOut = DateTime.Now,
            HourType = HourType.TechLead
        };

        _mockRepository.Setup(r => r.InsertPunch(punchInfo)).Verifiable();
        _mockRepository.Setup(r => r.GetLastPunch()).Returns(expectedRecord);

        // Act
        var result = _controller.PunchHours(punchInfo);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRecord = okResult.Value.Should().BeAssignableTo<PunchRecord>().Subject;
        returnedRecord.PunchOut.Should().NotBeNull();
        returnedRecord.HourType.Should().Be(HourType.TechLead);
    }

    #endregion

    #region GetLastPunch Tests

    [Test]
    public void GetLastPunch_ReturnsOkWithRecord_WhenRecordExists()
    {
        // Arrange
        var expectedRecord = new PunchRecord
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Now.AddHours(-2),
            PunchOut = null,
            HourType = HourType.Regular
        };

        _mockRepository
            .Setup(r => r.GetLastPunch())
            .Returns(expectedRecord);

        // Act
        var result = _controller.GetLastPunch();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(expectedRecord);
    }

    [Test]
    public void GetLastPunch_ReturnsNotFound_WhenNoRecordExists()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetLastPunch())
            .Returns((PunchRecord?)null);

        // Act
        var result = _controller.GetLastPunch();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = "No punch records found" });
    }

    [Test]
    public void GetLastPunch_ReturnsOpenPunch_WhenPunchOutIsNull()
    {
        // Arrange
        var openPunch = new PunchRecord
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Now.AddHours(-3),
            PunchOut = null,
            HourType = HourType.TechLead
        };

        _mockRepository
            .Setup(r => r.GetLastPunch())
            .Returns(openPunch);

        // Act
        var result = _controller.GetLastPunch();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRecord = okResult.Value.Should().BeAssignableTo<PunchRecord>().Subject;
        returnedRecord.PunchOut.Should().BeNull();
    }

    [Test]
    public void GetLastPunch_ReturnsClosedPunch_WhenPunchOutIsSet()
    {
        // Arrange
        var closedPunch = new PunchRecord
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Now.AddHours(-8),
            PunchOut = DateTime.Now,
            HourType = HourType.Regular
        };

        _mockRepository
            .Setup(r => r.GetLastPunch())
            .Returns(closedPunch);

        // Act
        var result = _controller.GetLastPunch();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRecord = okResult.Value.Should().BeAssignableTo<PunchRecord>().Subject;
        returnedRecord.PunchOut.Should().NotBeNull();
    }

    #endregion
}
