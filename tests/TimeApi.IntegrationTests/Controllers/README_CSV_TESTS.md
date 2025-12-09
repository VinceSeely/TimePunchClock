# CSV Upload Integration Tests

## Overview

This directory contains comprehensive integration tests for the CSV Upload functionality in the TimePunchClock API. These tests call the actual HTTP endpoints and validate the full request/response cycle, including database interactions.

## Test Files

### CsvUploadControllerIntegrationTests.cs
Main test suite with 30+ test cases covering all aspects of CSV upload functionality:

#### Template Download Tests (6 tests)
- `DownloadTemplate_ReturnsOkStatus` - Verifies successful template download
- `DownloadTemplate_ReturnsCorrectContentType` - Validates text/csv content type
- `DownloadTemplate_ReturnsCorrectFileName` - Checks filename is correct
- `DownloadTemplate_ContainsRequiredHeaders` - Ensures all required headers present
- `DownloadTemplate_ContainsExampleData` - Validates example data in template

#### Valid CSV Upload Tests (7 tests)
- `UploadCsv_WithValidFile_ReturnsOkStatus` - Basic valid upload
- `UploadCsv_WithValidFile_ReturnsCorrectImportResult` - Validates import counts
- `UploadCsv_WithOptionalPunchOut_SuccessfullyImports` - Tests optional PunchOut field
- `UploadCsv_WithDefaultHourType_UsesRegular` - Validates default HourType behavior
- `UploadCsv_WithLargeValidFile_SuccessfullyImports` - Tests 100 records
- `UploadCsv_WithQuotedFields_ParsesCorrectly` - Tests CSV field quoting
- `UploadCsv_WithValidHourTypes_SuccessfullyImports` - Tests case-insensitive HourType

#### Invalid File Tests (3 tests)
- `UploadCsv_WithNullFile_ReturnsBadRequest` - Tests missing file
- `UploadCsv_WithEmptyFile_ReturnsBadRequest` - Tests empty file
- `UploadCsv_WithNonCsvExtension_ReturnsBadRequest` - Tests wrong file extension
- `UploadCsv_WithExcessiveFileSize_ReturnsBadRequest` - Tests 10MB file size limit

#### Missing Required Columns Tests (3 tests)
- `UploadCsv_WithMissingPunchInColumn_ReturnsServerError` - Tests missing required column
- `UploadCsv_WithOnlyHeaderLine_ReturnsOk` - Tests header-only CSV
- `UploadCsv_WithEmptyHeaderLine_ReturnsServerError` - Tests completely empty file

#### Invalid Date Format Tests (3 tests)
- `UploadCsv_WithInvalidPunchInDate_ReturnsPartialSuccess` - Tests invalid PunchIn dates
- `UploadCsv_WithInvalidPunchOutDate_ReturnsPartialSuccess` - Tests invalid PunchOut dates
- `UploadCsv_WithMissingPunchInValue_ReturnsError` - Tests empty PunchIn value

#### Invalid HourType Tests (2 tests)
- `UploadCsv_WithInvalidHourType_ReturnsError` - Tests invalid HourType values
- `UploadCsv_WithValidHourTypes_SuccessfullyImports` - Tests all valid HourTypes

#### Maximum Records Tests (2 tests)
- `UploadCsv_WithExcessiveRecords_ReturnsBadRequest` - Tests 10,000 record limit
- `UploadCsv_WithExactlyMaxRecords_SuccessfullyImports` - Tests exactly at limit

#### Mixed Valid/Invalid Tests (2 tests)
- `UploadCsv_WithMixedValidAndInvalidRecords_ReturnsPartialSuccess` - Tests partial imports
- `UploadCsv_WithAllInvalidRecords_ReturnsAllErrors` - Tests all records invalid

#### Error Message Truncation Tests (1 test)
- `UploadCsv_WithMoreThan100Errors_TruncatesErrorList` - Tests error list truncation

#### Special Characters Tests (2 tests)
- `UploadCsv_WithSpecialCharactersInDescription_SuccessfullyImports` - Tests special chars
- `UploadCsv_WithBlankLines_IgnoresBlankLines` - Tests blank line handling

#### Real Sample File Test (1 test)
- `UploadCsv_WithRealSampleFile_SuccessfullyImports` - Tests with actual sample_punch_data.csv

### CsvUploadFileBasedTests.cs
Additional test suite using actual CSV files from the TestData directory:

- `UploadCsv_WithValidSampleFile_SuccessfullyImports` - Tests with valid_sample.csv
- `UploadCsv_WithInvalidDatesFile_ReturnsPartialSuccess` - Tests with invalid_dates.csv
- `UploadCsv_WithMissingPunchInColumnFile_ReturnsServerError` - Tests with missing_punchin_column.csv
- `UploadCsv_WithInvalidHourTypesFile_ReturnsPartialSuccess` - Tests with invalid_hourtypes.csv
- `UploadCsv_WithSpecialCharactersFile_SuccessfullyImports` - Tests with special_characters.csv
- `UploadCsv_WithRealSamplePunchData_SuccessfullyImports` - Tests with project's sample_punch_data.csv
- `UploadCsv_MultipleUploads_EachSucceeds` - Tests multiple sequential uploads

## Test Data Files

Located in `TestData/` directory:

1. **valid_sample.csv** - Valid CSV with 6 records matching sample_punch_data.csv
2. **invalid_dates.csv** - CSV with invalid date formats
3. **missing_punchin_column.csv** - CSV missing the required PunchIn column
4. **invalid_hourtypes.csv** - CSV with invalid HourType values
5. **special_characters.csv** - CSV with special characters, Unicode, and emojis

## Test Coverage

### Endpoints Tested
- `POST /api/csvupload/upload` - CSV file upload
- `GET /api/csvupload/template` - CSV template download

### Test Scenarios

#### Positive Tests
- Valid CSV with all fields
- Optional PunchOut field (work in progress)
- Default HourType (Regular when not specified)
- Large files (100+ records, up to 10,000)
- Quoted fields with commas
- Case-insensitive HourType values
- Special characters and Unicode in descriptions
- Blank lines in CSV

#### Negative Tests
- No file uploaded
- Empty file
- Non-CSV file extension
- File size exceeding 10MB limit
- Missing required PunchIn column
- Empty or missing header line
- Invalid PunchIn date format
- Invalid PunchOut date format
- Missing PunchIn value
- Invalid HourType values
- Exceeding 10,000 record limit

#### Edge Cases
- Exactly at 10,000 record limit
- Mixed valid and invalid records (partial success)
- All invalid records
- More than 100 errors (truncation)
- Multiple sequential uploads

## Running the Tests

### Run All CSV Upload Tests
```bash
dotnet test --filter "FullyQualifiedName~CsvUploadController"
```

### Run Specific Test Class
```bash
# In-memory tests
dotnet test --filter "FullyQualifiedName~CsvUploadControllerIntegrationTests"

# File-based tests
dotnet test --filter "FullyQualifiedName~CsvUploadFileBasedTests"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~UploadCsv_WithValidFile_ReturnsOkStatus"
```

### Run from Visual Studio
1. Open Test Explorer (Test → Test Explorer)
2. Navigate to TimeApi.IntegrationTests → Controllers
3. Right-click on test class or individual test
4. Select "Run" or "Debug"

## Test Infrastructure

### CustomWebApplicationFactory
- Uses in-memory database for fast, isolated tests
- Disables authentication for easier testing
- Each test gets a fresh database instance
- Automatic cleanup after each test

### Test Helpers
- `CreateMultipartFormData(fileName, content)` - Creates multipart form data for upload
- `CreateMultipartFormDataFromFile(filePath)` - Creates form data from actual file

## Expected Behavior

### Controller Constraints
- Maximum file size: 10 MB
- Maximum records per upload: 10,000
- Required column: PunchIn
- Optional columns: PunchOut, HourType, WorkDescription
- Valid HourTypes: Regular, TechLead (case-insensitive)
- Error list truncates at 100 errors

### Response Format
All upload responses return `CsvImportResult`:
```json
{
  "successCount": 5,
  "failureCount": 2,
  "errors": [
    "Line 2: Invalid PunchIn date format: not-a-date",
    "Line 4: Invalid HourType: Overtime. Valid values: TechLead, Regular"
  ],
  "isSuccess": true
}
```

### HTTP Status Codes
- 200 OK - File processed (may have partial failures)
- 400 Bad Request - Invalid file or too many records
- 500 Internal Server Error - Server error (missing columns, file parsing error)

## Authentication

Tests use the `CustomWebApplicationFactory` which bypasses authentication by:
1. Using in-memory database
2. Setting `User.Identity.IsAuthenticated = false`
3. Controller falls back to "dev-user" AuthId

In production, all endpoints require `[Authorize]` attribute.

## Maintenance

### Adding New Test Cases
1. Add test method to appropriate test class
2. Use descriptive test names following pattern: `MethodName_Scenario_ExpectedResult`
3. Follow Arrange-Act-Assert pattern
4. Use FluentAssertions for readable assertions
5. Add appropriate comments for complex scenarios

### Adding New Test Data Files
1. Create CSV file in `TestData/` directory
2. File automatically copied to output directory (configured in .csproj)
3. Add test case in `CsvUploadFileBasedTests.cs`

### Updating Tests After API Changes
1. Review controller changes
2. Update affected test assertions
3. Add new tests for new functionality
4. Update this README with new coverage

## Best Practices

1. **Test Independence** - Each test runs in isolation with fresh database
2. **Clear Naming** - Test names describe scenario and expected outcome
3. **Comprehensive Assertions** - Verify status codes, response content, and data
4. **Real HTTP Calls** - Tests call actual endpoints, not mocked methods
5. **Fast Execution** - In-memory database ensures quick test runs
6. **Maintainable** - Helper methods reduce duplication
7. **Well Documented** - Comments explain complex scenarios

## Troubleshooting

### Tests Fail to Find TestData Files
- Ensure TestData directory exists
- Verify .csproj includes `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`
- Check file paths in test assertions
- Run `dotnet build` to copy files

### Database-Related Failures
- Ensure Entity Framework packages are installed
- Check `CustomWebApplicationFactory` configuration
- Verify database is recreated for each test

### Authentication Issues
- Tests should bypass authentication via CustomWebApplicationFactory
- If seeing 401 errors, check factory configuration
- Verify [Authorize] attributes aren't blocking test requests

## Future Enhancements

Potential areas for additional test coverage:
- Performance tests with very large files (near 10MB limit)
- Concurrent upload tests (multiple simultaneous uploads)
- Database transaction rollback scenarios
- Different date format variations
- CSV files with different encodings (UTF-8, UTF-16, etc.)
- Malformed CSV structure tests
- Cross-browser file upload simulation
