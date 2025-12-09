using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeApi.Models;
using TimeApi.Services;

namespace TimeApi.Api;

[Route("api/[controller]")]
[ApiController]
public class CsvUploadController(
    ITimePunchRepository punchRepository,
    ILogger<CsvUploadController> logger) : ControllerBase
{
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxRecords = 10000; // Prevent excessive data uploads

    private string GetAuthId()
    {
        // Check if authentication is disabled (development mode)
        if (User.Identity?.IsAuthenticated != true)
        {
            return "dev-user";
        }

        // Try to get 'oid' (Object ID) claim from Azure AD
        var oidClaim = User.FindFirst("oid")
            ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");

        if (oidClaim != null)
            return oidClaim.Value;

        // Fallback to 'sub' (Subject) for other identity providers
        var subClaim = User.FindFirst("sub")
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (subClaim != null)
            return subClaim.Value;

        throw new UnauthorizedAccessException("User identifier not found in token claims");
    }

    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<CsvImportResult>> UploadCsv(IFormFile file)
    {
        var result = new CsvImportResult();

        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                result.Errors.Add("No file uploaded");
                return BadRequest(result);
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("File must be a CSV file");
                return BadRequest(result);
            }

            if (file.Length > MaxFileSizeBytes)
            {
                result.Errors.Add($"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB");
                return BadRequest(result);
            }

            var authId = GetAuthId();
            logger.LogInformation("Processing CSV upload for user: {AuthId}, file: {FileName}, size: {FileSize} bytes",
                authId, file.FileName, file.Length);

            // Parse CSV
            var records = await ParseCsvAsync(file);

            if (records.Count > MaxRecords)
            {
                result.Errors.Add($"CSV contains {records.Count} records, which exceeds the maximum of {MaxRecords}");
                return BadRequest(result);
            }

            // Convert and validate records
            var validEntities = new List<PunchEntity>();
            var lineNumber = 2; // Start at 2 (1 for header, +1 for current line)

            foreach (var record in records)
            {
                var (isValid, entity, error) = record.ToPunchEntity(authId);

                if (isValid && entity != null)
                {
                    validEntities.Add(entity);
                    result.SuccessCount++;
                }
                else
                {
                    result.FailureCount++;
                    result.Errors.Add($"Line {lineNumber}: {error}");
                }

                lineNumber++;
            }

            // Import valid records
            if (validEntities.Count > 0)
            {
                var insertedCount = await punchRepository.BulkInsertPunchesAsync(validEntities, authId);
                logger.LogInformation("Successfully imported {Count} punch records for user {AuthId}",
                    insertedCount, authId);
            }

            if (result.Errors.Count > 100)
            {
                var errorCount = result.Errors.Count;
                result.Errors = result.Errors.Take(100).ToList();
                result.Errors.Add($"... and {errorCount - 100} more errors");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing CSV upload");
            result.Errors.Add($"Server error: {ex.Message}");
            return StatusCode(500, result);
        }
    }

    private async Task<List<CsvPunchRecord>> ParseCsvAsync(IFormFile file)
    {
        var records = new List<CsvPunchRecord>();

        using var reader = new StreamReader(file.OpenReadStream());

        // Read header line
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("CSV file is empty");

        var headers = headerLine.Split(',').Select(h => h.Trim().Trim('"')).ToArray();

        // Expected headers: PunchIn, PunchOut, HourType, WorkDescription
        var punchInIndex = Array.FindIndex(headers, h => h.Equals("PunchIn", StringComparison.OrdinalIgnoreCase));
        var punchOutIndex = Array.FindIndex(headers, h => h.Equals("PunchOut", StringComparison.OrdinalIgnoreCase));
        var hourTypeIndex = Array.FindIndex(headers, h => h.Equals("HourType", StringComparison.OrdinalIgnoreCase));
        var workDescIndex = Array.FindIndex(headers, h => h.Equals("WorkDescription", StringComparison.OrdinalIgnoreCase));

        if (punchInIndex == -1)
            throw new InvalidOperationException("CSV must contain a 'PunchIn' column");

        // Read data lines
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseCsvLine(line);

            var record = new CsvPunchRecord
            {
                PunchIn = GetValueAt(values, punchInIndex),
                PunchOut = GetValueAt(values, punchOutIndex),
                HourType = GetValueAt(values, hourTypeIndex),
                WorkDescription = GetValueAt(values, workDescIndex)
            };

            records.Add(record);
        }

        return records;
    }

    private string?[] ParseCsvLine(string line)
    {
        var values = new List<string?>();
        var inQuotes = false;
        var currentValue = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.Trim());
                currentValue = string.Empty;
            }
            else
            {
                currentValue += c;
            }
        }

        values.Add(currentValue.Trim());
        return values.ToArray();
    }

    private string? GetValueAt(string?[] values, int index)
    {
        if (index < 0 || index >= values.Length)
            return null;

        var value = values[index];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [HttpGet("template")]
    [Authorize]
    public IActionResult DownloadTemplate()
    {
        var csv = "PunchIn,PunchOut,HourType,WorkDescription\n" +
                  "2024-01-01 09:00:00,2024-01-01 17:00:00,Regular,Working on project X\n" +
                  "2024-01-02 09:00:00,2024-01-02 18:00:00,TechLead,Code review and mentoring\n";

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "punch_import_template.csv");
    }
}
