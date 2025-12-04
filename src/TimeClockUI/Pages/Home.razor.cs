using Microsoft.AspNetCore.Components;
using TimeClock.Client;

namespace TimeClockUI.Pages;

public partial class Home
{
    [Inject] public TimePunchClient timePunchClient { get; set; } = null!;

    private string DisplayNextPunchStatus =>
        $"Please Punch {(punchIn ? "in" : "out")} last Punch time: {GetLastPunchTimeString()}";

    private string PunchButtonText => punchIn ? "Punch in" : "Punch Out";
    private bool punchIn => lastPunch.PunchOut != null;
    private HourType punchType;
    private bool ShowHours = false;
    private PunchRecord lastPunch = null!;
    private string techLeadHoursTotal = "0:00";
    private string regularHoursTotal = "0:00";
    private string combinedHoursTotal = "00:00";
    private IEnumerable<PunchRecord> todaysPunchs = null!;

    protected override async Task OnInitializedAsync()
    {
        lastPunch = await timePunchClient.GetLastPunch();
        todaysPunchs = await timePunchClient.GetTodaysPunchs();
        CalculateAndSetHours();
    }

    private string GetLastPunchTimeString() =>
        (lastPunch.PunchOut ?? lastPunch.PunchIn).ToLocalTime().ToString("MM/dd hh:mm tt");

    private string GetPunchTypeDisplayString(HourType type) =>
        type switch
        {
            HourType.Regular => "Regular Hours",
            HourType.TechLead => "Tech Lead Hours",
            _ => "Unknown type"
        };

    private async Task PunchIn()
    {
        lastPunch = await timePunchClient.Punch(punchType, PunchType.PunchIn);
        todaysPunchs = await timePunchClient.GetTodaysPunchs();
        CalculateAndSetHours();
        StateHasChanged();
    }

    private async Task PunchOut()
    {
        lastPunch = await timePunchClient.Punch(punchType, PunchType.PunchOut);
        todaysPunchs = await timePunchClient.GetTodaysPunchs();
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
}