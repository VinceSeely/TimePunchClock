using Microsoft.AspNetCore.Components;
using TimeClock.Client;
using TimeClockUI.Models;

namespace TimeClockUI.Pages;

public partial class MonthSummary
{
    [Inject] public TimePunchClient timePunchClient { get; set; }

    private List<MonthOption> Months = new()
    {
        new(month: 1, name: "January"),
        new(month: 2, name: "February"),
        new(month: 3, name: "March"),
        new(month: 4, name: "April"),
        new(month: 5, name: "May"),
        new(month: 6, name: "June"),
        new(month: 7, name: "July"),
        new(month: 8, name: "August"),
        new(month: 9, name: "September"),
        new(month: 10, name: "October"),
        new(month: 11, name: "November"),
        new(month: 12, name: "December")
    };

    private string techLeadHoursTotal = "0:00";
    private string regularHoursTotal = "0:00";
    private string combinedHoursTotal = "00:00";
    private IEnumerable<PunchRecord> todaysPunchs = null!;
    private int SelectedYear = 2025;
    private MonthOption? SelectedMonth;
    private DateTime StartDate;
    private DateTime EndDate;
    private List<int> Years = Enumerable.Range(2025, 20).ToList(); // 2025–2044

    protected override async Task OnInitializedAsync()
    {
        // Create July date range in CST

        await CalculateHours();
    }

    private async Task CalculateHours()
    {
        todaysPunchs = await timePunchClient.GetPunchesRange(StartDate, EndDate);
        CalculateAndSetHours();
    }

    private void CalculateAndSetHours()
    {
        var groupedHours = Enum.GetValues(typeof(HourType))
            .Cast<HourType>()
            .ToDictionary(hourType =>
                    hourType,
                hourType =>
                {
                    var totalMinutes = todaysPunchs
                        .Where(p => p.HourType == hourType && p.PunchOut.HasValue)
                        .Select(p => (p.PunchOut.Value - p.PunchIn).TotalMinutes)
                        .Sum();

                    var hours = (int)(totalMinutes / 60);
                    var minutes = (int)(totalMinutes % 60);

                    return (hours, minutes);
                });

        var (techHours, techMins) = groupedHours.GetValueOrDefault(HourType.TechLead, (0, 0));
        var (regHours, regMins) = groupedHours.GetValueOrDefault(HourType.Regular, (0, 0));

        techLeadHoursTotal = $"{techHours:D2}:{techMins:D2}";
        regularHoursTotal = $"{regHours:D2}:{regMins:D2}";

        var combinedMinutes = techHours * 60 + techMins + regHours * 60 + regMins;
        var totalHours = combinedMinutes / 60;
        var totalMins = combinedMinutes % 60;

        combinedHoursTotal = $"{totalHours:D2}:{totalMins:D2}";
    }

    private async Task OnYearChanged(int newYear)
    {
        SelectedYear = newYear;
        await UpdateDateRange();
    }

    private async Task OnMonthChanged(MonthOption newMonth)
    {
        SelectedMonth = newMonth;
        await UpdateDateRange();
    }

    private async Task UpdateDateRange()
    {
        if (SelectedMonth is null)
            return;

        // Start of month
        StartDate = new(SelectedYear, SelectedMonth.Month, 1);

        // End of month (handles leap years automatically)
        EndDate = StartDate.AddMonths(1).AddDays(-1);

        await CalculateHours();
    }
}