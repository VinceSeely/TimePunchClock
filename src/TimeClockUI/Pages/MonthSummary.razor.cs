using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TimeClock.Client;
using TimeClockUI.Models;

namespace TimeClockUI.Pages;

public partial class MonthSummary
{
    [Inject] public TimePunchClient TimePunchClient { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private List<MonthOption> _months = new()
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

    private string _techLeadHoursTotal = "0:00";
    private string _regularHoursTotal = "0:00";
    private string _combinedHoursTotal = "00:00";
    private IEnumerable<PunchRecord> _todaysPunchs = null!;
    private int _selectedYear = DateTime.Now.AddMonths(-1).Year;
    private int _selectedMonth;
    private DateTime _startDate;
    private DateTime _endDate;
    private List<int> _years = Enumerable.Range(2025, 20).ToList(); // 2025–2044
    private bool _isAuthenticated = false;

    protected override async Task OnInitializedAsync()
    {
        // Initialize with last month
        var lastMonth = DateTime.Now.AddMonths(-1);
        _selectedMonth = lastMonth.Month;
        _selectedYear = lastMonth.Year;

        _startDate = new(_selectedYear, _selectedMonth, 1);
        _endDate = _startDate.AddMonths(1).AddDays(-1);

        // Check if user is authenticated before making API calls
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        _isAuthenticated = user.Identity?.IsAuthenticated == true;

        if (_isAuthenticated)
        {
            await CalculateHours();
        }
        else
        {
            // User is not logged in, initialize with empty data
            _todaysPunchs = Enumerable.Empty<PunchRecord>();
        }
    }

    private async Task CalculateHours()
    {
        _todaysPunchs = await TimePunchClient.GetPunchesRange(_startDate, _endDate) ?? Enumerable.Empty<PunchRecord>();
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
                    var totalMinutes = _todaysPunchs
                        .Where(p => p.HourType == hourType && p.PunchOut.HasValue)
                        .Select(p => (p.PunchOut.Value - p.PunchIn).TotalMinutes)
                        .Sum();

                    var hours = (int)(totalMinutes / 60);
                    var minutes = (int)(totalMinutes % 60);

                    return (hours, minutes);
                });

        var (techHours, techMins) = groupedHours.GetValueOrDefault(HourType.TechLead, (0, 0));
        var (regHours, regMins) = groupedHours.GetValueOrDefault(HourType.Regular, (0, 0));

        _techLeadHoursTotal = $"{techHours:D2}:{techMins:D2}";
        _regularHoursTotal = $"{regHours:D2}:{regMins:D2}";

        var combinedMinutes = techHours * 60 + techMins + regHours * 60 + regMins;
        var totalHours = combinedMinutes / 60;
        var totalMins = combinedMinutes % 60;

        _combinedHoursTotal = $"{totalHours:D2}:{totalMins:D2}";
    }

    private async Task OnYearChanged(int newYear)
    {
        _selectedYear = newYear;
        await UpdateDateRange();
    }

    private async Task OnMonthChanged(int newMonth)
    {
        _selectedMonth = newMonth;
        await UpdateDateRange();
    }

    private async Task UpdateDateRange()
    {
        // Start of month
        _startDate = new(_selectedYear, _selectedMonth, 1);

        // End of month (handles leap years automatically)
        _endDate = _startDate.AddMonths(1).AddDays(-1);

        await CalculateHours();
    }
}