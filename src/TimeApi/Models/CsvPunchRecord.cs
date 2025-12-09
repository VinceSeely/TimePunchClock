using TimeClock.Client;

namespace TimeApi.Models;

/// <summary>
/// Represents a punch record from CSV import
/// Expected CSV format: PunchIn,PunchOut,HourType,WorkDescription
/// Note: PunchId, AuthId, CreatedAt, and UpdatedAt are generated automatically
/// </summary>
public class CsvPunchRecord
{
    public string? PunchIn { get; set; }
    public string? PunchOut { get; set; }
    public string? HourType { get; set; }
    public string? WorkDescription { get; set; }

    /// <summary>
    /// Validates and converts the CSV record to a PunchEntity
    /// </summary>
    /// <param name="authId">The authenticated user's ID - all imported records belong to this user</param>
    public (bool isValid, PunchEntity? entity, string? error) ToPunchEntity(string authId)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(PunchIn))
                return (false, null, "PunchIn is required");

            if (!DateTime.TryParse(PunchIn, out var punchInDate))
                return (false, null, $"Invalid PunchIn date format: {PunchIn}");

            // Parse optional PunchOut
            DateTime? punchOutDate = null;
            if (!string.IsNullOrWhiteSpace(PunchOut))
            {
                if (!DateTime.TryParse(PunchOut, out var parsedOut))
                    return (false, null, $"Invalid PunchOut date format: {PunchOut}");
                punchOutDate = parsedOut;
            }

            // Parse HourType (default to Regular if not specified)
            var hourType = TimeClock.Client.HourType.Regular;
            if (!string.IsNullOrWhiteSpace(HourType))
            {
                if (!Enum.TryParse<TimeClock.Client.HourType>(HourType, true, out var parsedType))
                    return (false, null, $"Invalid HourType: {HourType}. Valid values: TechLead, Regular");
                hourType = parsedType;
            }

            // Create entity - use current authenticated user's ID
            var entity = new PunchEntity
            {
                PunchId = Guid.NewGuid(),
                PunchIn = punchInDate,
                PunchOut = punchOutDate,
                HourType = hourType,
                AuthId = authId, // Use the authenticated user's ID
                WorkDescription = WorkDescription,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            return (true, entity, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Unexpected error: {ex.Message}");
        }
    }
}
