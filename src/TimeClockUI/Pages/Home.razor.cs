using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TimeClock.Client;

namespace TimeClockUI.Pages;

public partial class Home
{
    [Inject] public TimePunchClient TimePunchClient { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private string DisplayNextPunchStatus =>
        $"Please Punch {(_ShouldPunchIn ? "in" : "out")} last Punch time: {GetLastPunchTimeString()}";

    private string PunchButtonText => _ShouldPunchIn ? "Punch in" : "Punch Out";
    private bool _ShouldPunchIn=> _lastPunch?.PunchOut != null;
    private HourType _punchType;
    private bool _showHours = false;
    private PunchRecord? _lastPunch;
    private string _techLeadHoursTotal = "0:00";
    private string _regularHoursTotal = "0:00";
    private string _combinedHoursTotal = "00:00";
    private IEnumerable<PunchRecord> _todaysPunchs = null!;
    private bool _isAuthenticated = false;

    protected override async Task OnInitializedAsync()
    {
        // Check if user is authenticated before making API calls
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        _isAuthenticated = user.Identity?.IsAuthenticated == true;

        if (_isAuthenticated)
        {
            // User is logged in, safe to make API calls
            _lastPunch = await TimePunchClient.GetLastPunch();
            _todaysPunchs = await TimePunchClient.GetTodaysPunchs();
            CalculateAndSetHours();
        }
        else
        {
            // User is not logged in, initialize with empty data
            _todaysPunchs = Enumerable.Empty<PunchRecord>();
        }
    }

    private string GetLastPunchTimeString()
    {
        if (_lastPunch == null) return "N/A";
        var punchTime = _lastPunch.PunchOut ?? _lastPunch.PunchIn;
        return punchTime.ToLocalTime().ToString("MM/dd hh:mm tt");
    }

    private string GetPunchTypeDisplayString(HourType type) =>
        type switch
        {
            HourType.Regular => "Regular Hours",
            HourType.TechLead => "Tech Lead Hours",
            _ => "Unknown type"
        };

    private async Task PunchIn()
    {
        _lastPunch = await TimePunchClient.Punch(_punchType, PunchType.PunchIn);
        _todaysPunchs = await TimePunchClient.GetTodaysPunchs();
        CalculateAndSetHours();
        StateHasChanged();
    }

    private async Task PunchOut()
    {
        _lastPunch = await TimePunchClient.Punch(_punchType, PunchType.PunchOut);
        _todaysPunchs = await TimePunchClient.GetTodaysPunchs();
        CalculateAndSetHours();
        StateHasChanged();
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
}