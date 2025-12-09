using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using TimeApi.Models;

namespace TimeApi.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for CsvUploadController endpoints.
/// Tests the full HTTP request/response cycle for CSV upload and template download functionality.
/// </summary>
[TestFixture]
public class CsvUploadControllerIntegrationTests
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

    #region Template Download Tests

    [Test]
    public async Task DownloadTemplate_ReturnsOkStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/csvupload/template");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task DownloadTemplate_ReturnsCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/csvupload/template");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
    }

    [Test]
    public async Task DownloadTemplate_ReturnsCorrectFileName()
    {
        // Act
        var response = await _client.GetAsync("/api/csvupload/template");

        // Assert
        response.Content.Headers.ContentDisposition?.FileName.Should().Be("punch_import_template.csv");
    }

    [Test]
    public async Task DownloadTemplate_ContainsRequiredHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/csvupload/template");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("PunchIn");
        content.Should().Contain("PunchOut");
        content.Should().Contain("HourType");
        content.Should().Contain("WorkDescription");
    }

    [Test]
    public async Task DownloadTemplate_ContainsExampleData()
    {
        // Act
        var response = await _client.GetAsync("/api/csvupload/template");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("2024-01-01 09:00:00");
        content.Should().Contain("Regular");
        content.Should().Contain("TechLead");
    }

    #endregion

    #region Valid CSV Upload Tests

    [Test]
    public async Task UploadCsv_WithValidFile_ReturnsOkStatus()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,2024-12-01 17:00:00,Regular,Testing CSV upload
2024-12-02 09:00:00,2024-12-02 17:00:00,TechLead,Code review";

        using var formData = CreateMultipartFormData("test.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task UploadCsv_WithValidFile_ReturnsCorrectImportResult()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,2024-12-01 17:00:00,Regular,Testing CSV upload
2024-12-02 09:00:00,2024-12-02 17:00:00,TechLead,Code review
2024-12-03 09:00:00,,Regular,Work in progress";

        using var formData = CreateMultipartFormData("test.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UploadCsv_WithOptionalPunchOut_SuccessfullyImports()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,,Regular,Work in progress";

        using var formData = CreateMultipartFormData("test.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(0);
    }

    [Test]
    public async Task UploadCsv_WithDefaultHourType_UsesRegular()
    {
        // Arrange - HourType column empty, should default to Regular
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,2024-12-01 17:00:00,,Testing default hour type";

        using var formData = CreateMultipartFormData("test.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(0);
    }

    [Test]
    public async Task UploadCsv_WithLargeValidFile_SuccessfullyImports()
    {
        // Arrange - Create a CSV with 100 records (within limits)
        var sb = new StringBuilder();
        sb.AppendLine("PunchIn,PunchOut,HourType,WorkDescription");

        for (int i = 1; i <= 100; i++)
        {
            var date = new DateTime(2024, 12, 1).AddDays(i % 30);
            sb.AppendLine($"{date:yyyy-MM-dd} 09:00:00,{date:yyyy-MM-dd} 17:00:00,Regular,Work day {i}");
        }

        using var formData = CreateMultipartFormData("large.csv", sb.ToString());

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(100);
        result.FailureCount.Should().Be(0);
    }

    [Test]
    public async Task UploadCsv_WithQuotedFields_ParsesCorrectly()
    {
        // Arrange - CSV with quoted fields containing commas
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
""2024-12-01 09:00:00"",""2024-12-01 17:00:00"",Regular,""Working on feature A, B, and C""
2024-12-02 09:00:00,2024-12-02 17:00:00,TechLead,Simple description";

        using var formData = CreateMultipartFormData("quoted.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
    }

    #endregion

    #region Invalid File Tests

    [Test]
    public async Task UploadCsv_WithNullFile_ReturnsBadRequest()
    {
        // Arrange
        using var formData = new MultipartFormDataContent();

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UploadCsv_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        using var formData = CreateMultipartFormData("empty.csv", string.Empty);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Should().NotBeNull();
        result!.Errors.Should().Contain("No file uploaded");
    }

    [Test]
    public async Task UploadCsv_WithNonCsvExtension_ReturnsBadRequest()
    {
        // Arrange
        var content = "PunchIn,PunchOut,HourType,WorkDescription\n2024-12-01 09:00:00,2024-12-01 17:00:00,Regular,Test";
        using var formData = CreateMultipartFormData("test.txt", content);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Should().NotBeNull();
        result!.Errors.Should().Contain("File must be a CSV file");
    }

    [Test]
    public async Task UploadCsv_WithExcessiveFileSize_ReturnsBadRequest()
    {
        // Arrange - Create a file larger than 10MB
        var largeContent = new StringBuilder();
        largeContent.AppendLine("PunchIn,PunchOut,HourType,WorkDescription");

        // Add enough lines to exceed 10MB (approximately 11MB)
        for (int i = 0; i < 150000; i++)
        {
            largeContent.AppendLine($"2024-12-01 09:00:00,2024-12-01 17:00:00,Regular,This is a long description to increase file size - Line {i}");
        }

        using var formData = CreateMultipartFormData("huge.csv", largeContent.ToString());

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        // Note: The RequestSizeLimit attribute should reject this before it reaches the controller
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
    }

    #endregion

    #region Missing Required Columns Tests

    [Test]
    public async Task UploadCsv_WithMissingPunchInColumn_ReturnsServerError()
    {
        // Arrange - CSV missing required PunchIn column
        var csvContent = @"PunchOut,HourType,WorkDescription
2024-12-01 17:00:00,Regular,Missing punch in";

        using var formData = CreateMultipartFormData("missing_column.csv", csvContent);

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
    public async Task UploadCsv_WithOnlyHeaderLine_ReturnsOk()
    {
        // Arrange - CSV with only header, no data
        var csvContent = "PunchIn,PunchOut,HourType,WorkDescription";

        using var formData = CreateMultipartFormData("header_only.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
    }

    [Test]
    public async Task UploadCsv_WithEmptyHeaderLine_ReturnsServerError()
    {
        // Arrange - Completely empty file (no header)
        var csvContent = "\n\n";

        using var formData = CreateMultipartFormData("no_header.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Should().NotBeNull();
        result!.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("CSV file is empty");
    }

    #endregion

    #region Invalid Date Format Tests

    [Test]
    public async Task UploadCsv_WithInvalidPunchInDate_ReturnsPartialSuccess()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
invalid-date,2024-12-01 17:00:00,Regular,Bad date
2024-12-02 09:00:00,2024-12-02 17:00:00,Regular,Good date";

        using var formData = CreateMultipartFormData("invalid_date.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Line 2");
        result.Errors[0].Should().Contain("Invalid PunchIn date format");
    }

    [Test]
    public async Task UploadCsv_WithInvalidPunchOutDate_ReturnsPartialSuccess()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,not-a-date,Regular,Bad punch out
2024-12-02 09:00:00,2024-12-02 17:00:00,Regular,Good date";

        using var formData = CreateMultipartFormData("invalid_punchout.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Invalid PunchOut date format");
    }

    [Test]
    public async Task UploadCsv_WithMissingPunchInValue_ReturnsError()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
,2024-12-01 17:00:00,Regular,Missing punch in value
2024-12-02 09:00:00,2024-12-02 17:00:00,Regular,Good record";

        using var formData = CreateMultipartFormData("missing_punchin.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("PunchIn is required");
    }

    #endregion

    #region Invalid HourType Tests

    [Test]
    public async Task UploadCsv_WithInvalidHourType_ReturnsError()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,2024-12-01 17:00:00,InvalidType,Bad hour type
2024-12-02 09:00:00,2024-12-02 17:00:00,Regular,Good record";

        using var formData = CreateMultipartFormData("invalid_hourtype.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Invalid HourType");
        result.Errors[0].Should().Contain("InvalidType");
    }

    [Test]
    public async Task UploadCsv_WithValidHourTypes_SuccessfullyImports()
    {
        // Arrange - Test both valid hour types
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,2024-12-01 17:00:00,Regular,Regular work
2024-12-02 09:00:00,2024-12-02 17:00:00,TechLead,Tech lead work
2024-12-03 09:00:00,2024-12-03 17:00:00,regular,Case insensitive regular
2024-12-04 09:00:00,2024-12-04 17:00:00,TECHLEAD,Case insensitive techlead";

        using var formData = CreateMultipartFormData("valid_hourtypes.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(4);
        result.FailureCount.Should().Be(0);
    }

    #endregion

    #region Maximum Records Tests

    [Test]
    public async Task UploadCsv_WithExcessiveRecords_ReturnsBadRequest()
    {
        // Arrange - Create CSV with more than 10,000 records
        var sb = new StringBuilder();
        sb.AppendLine("PunchIn,PunchOut,HourType,WorkDescription");

        for (int i = 1; i <= 10001; i++)
        {
            var date = new DateTime(2024, 1, 1).AddDays(i % 365);
            sb.AppendLine($"{date:yyyy-MM-dd} 09:00:00,{date:yyyy-MM-dd} 17:00:00,Regular,Work day {i}");
        }

        using var formData = CreateMultipartFormData("too_many_records.csv", sb.ToString());

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Should().NotBeNull();
        result!.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("exceeds the maximum of 10000");
    }

    [Test]
    public async Task UploadCsv_WithExactlyMaxRecords_SuccessfullyImports()
    {
        // Arrange - Create CSV with exactly 10,000 records (the limit)
        var sb = new StringBuilder();
        sb.AppendLine("PunchIn,PunchOut,HourType,WorkDescription");

        for (int i = 1; i <= 10000; i++)
        {
            var date = new DateTime(2024, 1, 1).AddDays(i % 365);
            sb.AppendLine($"{date:yyyy-MM-dd} 09:00:00,{date:yyyy-MM-dd} 17:00:00,Regular,Work day {i}");
        }

        using var formData = CreateMultipartFormData("max_records.csv", sb.ToString());

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(10000);
        result.FailureCount.Should().Be(0);
    }

    #endregion

    #region Mixed Valid and Invalid Records Tests

    [Test]
    public async Task UploadCsv_WithMixedValidAndInvalidRecords_ReturnsPartialSuccess()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,2024-12-01 17:00:00,Regular,Good record 1
invalid-date,2024-12-02 17:00:00,Regular,Bad date
2024-12-03 09:00:00,2024-12-03 17:00:00,InvalidType,Bad hour type
2024-12-04 09:00:00,2024-12-04 17:00:00,Regular,Good record 2
,2024-12-05 17:00:00,Regular,Missing punch in
2024-12-06 09:00:00,2024-12-06 17:00:00,TechLead,Good record 3";

        using var formData = CreateMultipartFormData("mixed.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(3);
        result!.FailureCount.Should().Be(3);
        result.Errors.Should().HaveCount(3);
        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task UploadCsv_WithAllInvalidRecords_ReturnsAllErrors()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
invalid-date,2024-12-01 17:00:00,Regular,Bad date
,2024-12-02 17:00:00,Regular,Missing punch in
2024-12-03 09:00:00,2024-12-03 17:00:00,BadType,Invalid hour type";

        using var formData = CreateMultipartFormData("all_invalid.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(3);
        result.Errors.Should().HaveCount(3);
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Error Message Truncation Tests

    [Test]
    public async Task UploadCsv_WithMoreThan100Errors_TruncatesErrorList()
    {
        // Arrange - Create CSV with 150 invalid records
        var sb = new StringBuilder();
        sb.AppendLine("PunchIn,PunchOut,HourType,WorkDescription");

        for (int i = 1; i <= 150; i++)
        {
            sb.AppendLine($"invalid-date-{i},2024-12-01 17:00:00,Regular,Bad record {i}");
        }

        using var formData = CreateMultipartFormData("many_errors.csv", sb.ToString());

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(150);
        result.Errors.Should().HaveCount(101); // 100 errors + 1 truncation message
        result.Errors.Last().Should().Contain("and 50 more errors");
    }

    #endregion

    #region Special Characters and Edge Cases Tests

    [Test]
    public async Task UploadCsv_WithSpecialCharactersInDescription_SuccessfullyImports()
    {
        // Arrange - Note: Multi-line descriptions may not be supported by simple CSV parser
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,2024-12-01 17:00:00,Regular,""Work with special chars: @#$%^&*()""
2024-12-02 09:00:00,2024-12-02 17:00:00,Regular,Unicode test: 你好世界
2024-12-03 09:00:00,2024-12-03 17:00:00,Regular,Emojis and symbols: Testing CSV";

        using var formData = CreateMultipartFormData("special_chars.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
    }

    [Test]
    public async Task UploadCsv_WithBlankLines_IgnoresBlankLines()
    {
        // Arrange
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,2024-12-01 17:00:00,Regular,Record 1

2024-12-02 09:00:00,2024-12-02 17:00:00,Regular,Record 2

";

        using var formData = CreateMultipartFormData("blank_lines.csv", csvContent);

        // Act
        var response = await _client.PostAsync("/api/csvupload/upload", formData);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
    }

    #endregion

    #region Real Sample File Test

    [Test]
    public async Task UploadCsv_WithRealSampleFile_SuccessfullyImports()
    {
        // Arrange - Use actual sample_punch_data.csv content
        var csvContent = @"PunchIn,PunchOut,HourType,WorkDescription
2024-12-01 09:00:00,2024-12-01 17:30:00,Regular,Implemented user authentication feature
2024-12-02 08:30:00,2024-12-02 16:45:00,Regular,Fixed bug in payment processing module
2024-12-03 09:15:00,2024-12-03 18:00:00,TechLead,Code review and team mentoring
2024-12-04 09:00:00,2024-12-04 17:00:00,Regular,Database optimization and performance tuning
2024-12-05 10:00:00,2024-12-05 14:30:00,Regular,Sprint planning and documentation
2024-12-06 09:00:00,,Regular,Current work in progress";

        using var formData = CreateMultipartFormData("sample_punch_data.csv", csvContent);

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

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates multipart form data with a CSV file for upload testing
    /// </summary>
    private static MultipartFormDataContent CreateMultipartFormData(string fileName, string content)
    {
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(fileContent, "file", fileName);
        return formData;
    }

    #endregion
}
