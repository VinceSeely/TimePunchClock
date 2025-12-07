using TimeApi.Models;
using TimeApi.Tests.Builders;
using TimeClock.Client;

namespace TimeApi.Tests.Fixtures;

/// <summary>
/// Factory methods for common test data scenarios.
/// Use these for quick test data creation, or use the builders for more control.
/// </summary>
public static class TestDataFactory
{
    // Common test user IDs
    public const string TestAuthId1 = "user-123-abc";
    public const string TestAuthId2 = "user-456-def";

    /// <summary>
    /// Creates a simple open punch from today
    /// </summary>
    public static PunchEntity CreateOpenPunch(HourType hourType = HourType.Regular, string? authId = null)
    {
        return new PunchEntityBuilder()
            .WithPunchIn(DateTime.Now)
            .WithHourType(hourType)
            .WithAuthId(authId)
            .AsOpenPunch()
            .Build();
    }

    /// <summary>
    /// Creates a closed punch for today (8 hour shift)
    /// </summary>
    public static PunchEntity CreateClosedPunch(
        DateTime? punchIn = null,
        HourType hourType = HourType.Regular,
        string? authId = null,
        string? workDescription = null)
    {
        var punchInTime = punchIn ?? DateTime.Today.AddHours(9); // 9 AM
        return new PunchEntityBuilder()
            .WithPunchIn(punchInTime)
            .WithPunchOut(punchInTime.AddHours(8))
            .WithHourType(hourType)
            .WithAuthId(authId)
            .WithWorkDescription(workDescription)
            .Build();
    }

    /// <summary>
    /// Creates a punch record for a specific date range
    /// </summary>
    public static PunchEntity CreatePunchForDateRange(DateTime start, DateTime end, HourType hourType = HourType.Regular)
    {
        return new PunchEntityBuilder()
            .WithPunchIn(start)
            .WithPunchOut(end)
            .WithHourType(hourType)
            .Build();
    }

    /// <summary>
    /// Creates multiple punch records for a week (Mon-Fri, 8 hours each)
    /// </summary>
    public static List<PunchEntity> CreateWeekOfPunches(
        DateTime weekStart,
        string? authId = null,
        HourType hourType = HourType.Regular)
    {
        var punches = new List<PunchEntity>();

        for (int i = 0; i < 5; i++) // Mon-Fri
        {
            var day = weekStart.AddDays(i);
            var punchIn = day.Date.AddHours(9); // 9 AM
            var punchOut = punchIn.AddHours(8); // 5 PM

            punches.Add(new PunchEntityBuilder()
                .WithPunchIn(punchIn)
                .WithPunchOut(punchOut)
                .WithHourType(hourType)
                .WithAuthId(authId)
                .Build());
        }

        return punches;
    }

    /// <summary>
    /// Creates punches with different hour types (Regular and TechLead)
    /// </summary>
    public static List<PunchEntity> CreatePunchesWithVariousHourTypes(DateTime date, string? authId = null)
    {
        return new List<PunchEntity>
        {
            CreateClosedPunch(date.AddHours(8), HourType.Regular, authId, "Regular work"),
            CreateClosedPunch(date.AddDays(1).AddHours(8), HourType.TechLead, authId, "Tech lead duties")
        };
    }

    /// <summary>
    /// Creates a PunchInfo for testing punch endpoint
    /// </summary>
    public static PunchInfo CreatePunchInfo(
        PunchType punchType = PunchType.PunchIn,
        HourType hourType = HourType.Regular,
        string? workDescription = null)
    {
        return new PunchInfo
        {
            PunchType = punchType,
            HourType = hourType,
            WorkDescription = workDescription
        };
    }

    /// <summary>
    /// Creates legacy punch (no AuthId or WorkDescription) for backwards compatibility testing
    /// </summary>
    public static PunchEntity CreateLegacyPunch(DateTime punchIn, DateTime? punchOut = null)
    {
        return new PunchEntityBuilder()
            .WithPunchIn(punchIn)
            .WithPunchOut(punchOut ?? punchIn.AddHours(8))
            .WithAuthId(null)
            .WithWorkDescription(null)
            .Build();
    }
}
