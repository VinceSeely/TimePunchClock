using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TimeApi.Api;
using TimeApi.Services;
using TimeClock.Client;

namespace TimeApi.Tests.Api;

[TestFixture]
public class TimePunchControllerTests
{
    private Mock<ITimePunchRepository> _mockRepository = null!;
    private TimePunchController _controller = null!;
    private const string TestAuthId = "test-user-123";

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<ITimePunchRepository>(MockBehavior.Strict);
        _controller = new TimePunchController(_mockRepository.Object);

        // Setup mock user with claims
        var claims = new List<Claim>
        {
            new Claim("oid", TestAuthId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
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
            .Setup(r => r.GetPunchRecords(start, end, TestAuthId))
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
            .Setup(r => r.GetPunchRecords(start, end, TestAuthId))
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
            .Setup(r => r.GetPunchRecords(start, end, TestAuthId))
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
            .Setup(r => r.GetPunchRecords(start, end, TestAuthId))
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
            .Setup(r => r.InsertPunch(punchInfo, TestAuthId))
            .Verifiable();

        _mockRepository
            .Setup(r => r.GetLastPunch(TestAuthId))
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
            .Setup(r => r.InsertPunch(punchInfo, TestAuthId))
            .Verifiable();

        _mockRepository
            .InSequence(sequence)
            .Setup(r => r.GetLastPunch(TestAuthId))
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

        _mockRepository.Setup(r => r.InsertPunch(punchInfo, TestAuthId)).Verifiable();
        _mockRepository.Setup(r => r.GetLastPunch(TestAuthId)).Returns(expectedRecord);

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

        _mockRepository.Setup(r => r.InsertPunch(punchInfo, TestAuthId)).Verifiable();
        _mockRepository.Setup(r => r.GetLastPunch(TestAuthId)).Returns(expectedRecord);

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
            .Setup(r => r.GetLastPunch(TestAuthId))
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
            .Setup(r => r.GetLastPunch(TestAuthId))
            .Returns((PunchRecord?)null);

        // Act
        var result = _controller.GetLastPunch();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = "No punch records found" });
    }

    [Test]
    public void GetLastPunch_Controller_ReturnsOpenPunch_WhenPunchOutIsNull()
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
            .Setup(r => r.GetLastPunch(TestAuthId))
            .Returns(openPunch);

        // Act
        var result = _controller.GetLastPunch();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRecord = okResult.Value.Should().BeAssignableTo<PunchRecord>().Subject;
        returnedRecord.PunchOut.Should().BeNull();
    }

    [Test]
    public void GetLastPunch_Controller_ReturnsClosedPunch_WhenPunchOutIsSet()
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
            .Setup(r => r.GetLastPunch(TestAuthId))
            .Returns(closedPunch);

        // Act
        var result = _controller.GetLastPunch();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRecord = okResult.Value.Should().BeAssignableTo<PunchRecord>().Subject;
        returnedRecord.PunchOut.Should().NotBeNull();
    }

    #endregion

    #region UpdatePunch Tests

    [Test]
    public void UpdatePunch_ReturnsOkWithUpdatedRecord_WhenUpdateSucceeds()
    {
        // Arrange
        var updateDto = new PunchUpdateDto
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Today.AddHours(9).AddMinutes(15),
            PunchOut = DateTime.Today.AddHours(17).AddMinutes(15),
            HourType = HourType.Regular
        };

        var updatedRecord = new PunchRecord
        {
            PunchId = updateDto.PunchId,
            PunchIn = updateDto.PunchIn,
            PunchOut = updateDto.PunchOut,
            HourType = HourType.Regular
        };

        _mockRepository
            .Setup(r => r.UpdatePunch(updateDto, TestAuthId))
            .Returns(updatedRecord);

        // Act
        var result = _controller.UpdatePunch(updateDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(updatedRecord);
    }

    [Test]
    public void UpdatePunch_ReturnsNotFound_WhenPunchNotFound()
    {
        // Arrange
        var updateDto = new PunchUpdateDto
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Today.AddHours(9),
            PunchOut = DateTime.Today.AddHours(17),
            HourType = HourType.Regular
        };

        _mockRepository
            .Setup(r => r.UpdatePunch(updateDto, TestAuthId))
            .Throws(new InvalidOperationException("Punch record not found"));

        // Act
        var result = _controller.UpdatePunch(updateDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = "Punch record not found" });
    }

    [Test]
    public void UpdatePunch_ReturnsUnauthorized_WhenUserNotAuthorized()
    {
        // Arrange
        var updateDto = new PunchUpdateDto
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Today.AddHours(9),
            PunchOut = DateTime.Today.AddHours(17),
            HourType = HourType.Regular
        };

        _mockRepository
            .Setup(r => r.UpdatePunch(updateDto, TestAuthId))
            .Throws(new UnauthorizedAccessException("You are not authorized to update this punch record"));

        // Act
        var result = _controller.UpdatePunch(updateDto);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "You are not authorized to update this punch record" });
    }

    [Test]
    public void UpdatePunch_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var updateDto = new PunchUpdateDto
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Today.AddHours(17),
            PunchOut = DateTime.Today.AddHours(9), // Invalid: out before in
            HourType = HourType.Regular
        };

        _mockRepository
            .Setup(r => r.UpdatePunch(updateDto, TestAuthId))
            .Throws(new ArgumentException("Punch out time must be after punch in time"));

        // Act
        var result = _controller.UpdatePunch(updateDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = "Punch out time must be after punch in time" });
    }

    [Test]
    public void UpdatePunch_CallsRepositoryWithCorrectAuthId()
    {
        // Arrange
        var updateDto = new PunchUpdateDto
        {
            PunchId = Guid.NewGuid(),
            PunchIn = DateTime.Today.AddHours(9),
            PunchOut = DateTime.Today.AddHours(17),
            HourType = HourType.Regular
        };

        var updatedRecord = new PunchRecord
        {
            PunchId = updateDto.PunchId,
            PunchIn = updateDto.PunchIn,
            PunchOut = updateDto.PunchOut,
            HourType = HourType.Regular
        };

        _mockRepository
            .Setup(r => r.UpdatePunch(
                It.Is<PunchUpdateDto>(dto =>
                    dto.PunchId == updateDto.PunchId &&
                    dto.PunchIn == updateDto.PunchIn &&
                    dto.PunchOut == updateDto.PunchOut &&
                    dto.HourType == updateDto.HourType),
                TestAuthId))
            .Returns(updatedRecord)
            .Verifiable();

        // Act
        _controller.UpdatePunch(updateDto);

        // Assert verified by MockBehavior.Strict and VerifyAll() in TearDown
    }

    #endregion

    #region DeletePunch Tests

    [Test]
    public void DeletePunch_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var punchId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.DeletePunch(punchId, TestAuthId))
            .Verifiable();

        // Act
        var result = _controller.DeletePunch(punchId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public void DeletePunch_ReturnsNotFound_WhenPunchNotFound()
    {
        // Arrange
        var punchId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.DeletePunch(punchId, TestAuthId))
            .Throws(new InvalidOperationException("Punch record not found"));

        // Act
        var result = _controller.DeletePunch(punchId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = "Punch record not found" });
    }

    [Test]
    public void DeletePunch_ReturnsUnauthorized_WhenUserNotAuthorized()
    {
        // Arrange
        var punchId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.DeletePunch(punchId, TestAuthId))
            .Throws(new UnauthorizedAccessException("You are not authorized to delete this punch record"));

        // Act
        var result = _controller.DeletePunch(punchId);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "You are not authorized to delete this punch record" });
    }

    [Test]
    public void DeletePunch_CallsRepositoryWithCorrectAuthId()
    {
        // Arrange
        var punchId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.DeletePunch(punchId, TestAuthId))
            .Verifiable();

        // Act
        _controller.DeletePunch(punchId);

        // Assert verified by MockBehavior.Strict and VerifyAll() in TearDown
    }

    #endregion
}
