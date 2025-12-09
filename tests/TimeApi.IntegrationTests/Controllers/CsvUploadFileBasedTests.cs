using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using TimeApi.Models;

namespace TimeApi.IntegrationTests.Controllers;

/// <summary>
/// Additional integration tests for CsvUploadController using actual test files.
/// These tests use pre-created CSV files from the TestData directory.
/// </summary>
[TestFixture]
public class CsvUploadFileBasedTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _testDataPath = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        // Get the path to the TestData directory
        _testDataPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "TestData");

        // Normalize the path for Windows
        _testDataPath = Path.GetFullPath(_testDataPath);
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task UploadCsv_WithValidSampleFile_SuccessfullyImports()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_sample.csv");

        if (!File.Exists(filePath))
        {
            Assert.Inconclusive($"Test file not found: {filePath}");
            return;
        }

        using var formData = await CreateMultipartFormDataFromFile(filePath);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(6);
        result.FailureCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UploadCsv_WithInvalidDatesFile_ReturnsPartialSuccess()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "invalid_dates.csv");

        if (!File.Exists(filePath))
        {
            Assert.Inconclusive($"Test file not found: {filePath}");
            return;
        }

        using var formData = await CreateMultipartFormDataFromFile(filePath);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(2);
        result.Errors.Should().HaveCount(2);
    }

    [Test]
    public async Task UploadCsv_WithMissingPunchInColumnFile_ReturnsServerError()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "missing_punchin_column.csv");

        if (!File.Exists(filePath))
        {
            Assert.Inconclusive($"Test file not found: {filePath}");
            return;
        }

        using var formData = await CreateMultipartFormDataFromFile(filePath);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Should().NotBeNull();
        result!.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("must contain a 'PunchIn' column");
    }

    [Test]
    public async Task UploadCsv_WithInvalidHourTypesFile_ReturnsPartialSuccess()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "invalid_hourtypes.csv");

        if (!File.Exists(filePath))
        {
            Assert.Inconclusive($"Test file not found: {filePath}");
            return;
        }

        using var formData = await CreateMultipartFormDataFromFile(filePath);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(2);
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Contains("Invalid HourType"));
    }

    [Test]
    public async Task UploadCsv_WithSpecialCharactersFile_SuccessfullyImports()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "special_characters.csv");

        if (!File.Exists(filePath))
        {
            Assert.Inconclusive($"Test file not found: {filePath}");
            return;
        }

        using var formData = await CreateMultipartFormDataFromFile(filePath);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(4);
        result.FailureCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UploadCsv_WithRealSamplePunchData_SuccessfullyImports()
    {
        // Arrange - Test with the actual sample_punch_data.csv from the project root
        var projectRoot = Path.Combine(_testDataPath, "..", "..", "..", "..");
        var filePath = Path.Combine(projectRoot, "sample_punch_data.csv");
        filePath = Path.GetFullPath(filePath);

        if (!File.Exists(filePath))
        {
            Assert.Inconclusive($"Sample file not found: {filePath}");
            return;
        }

        using var formData = await CreateMultipartFormDataFromFile(filePath);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(6);
        result.FailureCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UploadCsv_MultipleUploads_EachSucceeds()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_sample.csv");

        if (!File.Exists(filePath))
        {
            Assert.Inconclusive($"Test file not found: {filePath}");
            return;
        }

        // Act - Upload the same file multiple times
        for (int i = 0; i < 3; i++)
        {
            using var formData = await CreateMultipartFormDataFromFile(filePath);
            var response = await _client.PostAsync("/api/csvupload/upload", formData);
            var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.SuccessCount.Should().Be(6, $"upload iteration {i + 1} should succeed");
            result.FailureCount.Should().Be(0);
        }
    }

    /// <summary>
    /// Creates multipart form data from an actual file on disk
    /// </summary>
    private static async Task<MultipartFormDataContent> CreateMultipartFormDataFromFile(string filePath)
    {
        var formData = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(fileContent, "file", Path.GetFileName(filePath));
        return formData;
    }
}
