using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using TimeClock.Client;
using TimeClockUI.Services;

namespace TimeClockUI.Pages;

public partial class WeekSummary
{
    [Inject] public TimePunchClient TimePunchClient { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public LoadingService LoadingService { get; set; } = null!;

    private List<PunchEditState> _punchEditStates = new();
    private DateTime _weekStart;
    private DateTime _weekEnd;
    private bool _isAuthenticated = false;

    private int UnsavedChangeCount => _punchEditStates.Count(x => x.IsDirty);

    protected override async Task OnInitializedAsync()
    {
        // Initialize to current week (Monday-Sunday)
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Sunday is 0, adjust to Monday = 0
        _weekStart = today.AddDays(-daysFromMonday);
        _weekEnd = _weekStart.AddDays(6);

        // Check if user is authenticated before making API calls
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        _isAuthenticated = user.Identity?.IsAuthenticated == true;

        if (_isAuthenticated)
        {
            await LoadWeekData();
        }
    }

    private async Task LoadWeekData()
    {
        using (LoadingService.Track())
        {
            var punches = await TimePunchClient.GetPunchesRange(_weekStart, _weekEnd.AddDays(1)) ?? Enumerable.Empty<PunchRecord>();
            _punchEditStates = punches.Select(p => new PunchEditState
            {
                Original = p,
                Current = new PunchRecord
                {
                    PunchId = p.PunchId,
                    PunchIn = p.PunchIn,
                    PunchOut = p.PunchOut,
                    HourType = p.HourType,
                    AuthId = p.AuthId,
                    WorkDescription = p.WorkDescription
                },
                IsEditing = false
            }).ToList();
        }
    }

    private async Task NavigateToPreviousWeek()
    {
        _weekStart = _weekStart.AddDays(-7);
        _weekEnd = _weekEnd.AddDays(-7);
        await LoadWeekData();
    }

    private async Task NavigateToNextWeek()
    {
        _weekStart = _weekStart.AddDays(7);
        _weekEnd = _weekEnd.AddDays(7);
        await LoadWeekData();
    }

    private async Task NavigateToCurrentWeek()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        _weekStart = today.AddDays(-daysFromMonday);
        _weekEnd = _weekStart.AddDays(6);
        await LoadWeekData();
    }

    private void StartEditing(PunchEditState state)
    {
        state.IsEditing = true;
        state.ValidationErrors.Clear();
    }

    private void CancelEditing(PunchEditState state)
    {
        state.Current = new PunchRecord
        {
            PunchId = state.Original.PunchId,
            PunchIn = state.Original.PunchIn,
            PunchOut = state.Original.PunchOut,
            HourType = state.Original.HourType,
            AuthId = state.Original.AuthId,
            WorkDescription = state.Original.WorkDescription
        };
        state.IsEditing = false;
        state.ValidationErrors.Clear();
    }

    private bool ValidatePunch(PunchEditState state)
    {
        state.ValidationErrors.Clear();

        if (state.Current.PunchOut.HasValue && state.Current.PunchOut.Value <= state.Current.PunchIn)
        {
            state.ValidationErrors["PunchOut"] = "Punch out must be after punch in";
            return false;
        }

        return true;
    }

    private async Task SavePunch(PunchEditState state)
    {
        if (!ValidatePunch(state))
        {
            return;
        }

        try
        {
            var updateDto = new PunchUpdateDto
            {
                PunchId = state.Current.PunchId,
                PunchIn = state.Current.PunchIn,
                PunchOut = state.Current.PunchOut ?? DateTime.Now,
                HourType = state.Current.HourType
            };

            var result = await TimePunchClient.UpdatePunch(updateDto);

            if (result != null)
            {
                state.Original = result;
                state.Current = new PunchRecord
                {
                    PunchId = result.PunchId,
                    PunchIn = result.PunchIn,
                    PunchOut = result.PunchOut,
                    HourType = result.HourType,
                    AuthId = result.AuthId,
                    WorkDescription = result.WorkDescription
                };
                state.IsEditing = false;
                Snackbar.Add("Changes saved successfully", Severity.Success);
            }
            else
            {
                Snackbar.Add("Failed to save changes", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving changes: {ex.Message}", Severity.Error);
        }
    }

    private async Task SaveAllChanges()
    {
        var dirtyStates = _punchEditStates.Where(s => s.IsDirty).ToList();

        foreach (var state in dirtyStates)
        {
            await SavePunch(state);
        }
    }

    private async Task DeletePunch(PunchEditState state)
    {
        try
        {
            var success = await TimePunchClient.DeletePunch(state.Current.PunchId);

            if (success)
            {
                _punchEditStates.Remove(state);
                Snackbar.Add("Punch deleted successfully", Severity.Success);
            }
            else
            {
                Snackbar.Add("Failed to delete punch", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting punch: {ex.Message}", Severity.Error);
        }
    }

    private string CalculateDuration(PunchRecord punch)
    {
        if (!punch.PunchOut.HasValue)
            return "--";

        var duration = punch.PunchOut.Value - punch.PunchIn;
        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }

    private string GetDayLabel(DateTime date)
    {
        var localDate = date.ToLocalTime();
        var dayOfWeek = localDate.DayOfWeek.ToString().Substring(0, 3);
        return $"{dayOfWeek} {localDate.ToString("M/d")}";
    }

    private TimeSpan? GetPunchInTimeSpan(PunchEditState state)
    {
        return state.Current.PunchIn.ToLocalTime().TimeOfDay;
    }

    private void SetPunchInTimeSpan(PunchEditState state, TimeSpan? value)
    {
        if (value.HasValue)
        {
            // Get the local date and combine with the new time
            var localDate = state.Current.PunchIn.ToLocalTime().Date + value.Value;
            // Convert back to UTC for storage
            var utcDate = localDate.ToUniversalTime();
            state.Current = new PunchRecord
            {
                PunchId = state.Current.PunchId,
                PunchIn = utcDate,
                PunchOut = state.Current.PunchOut,
                HourType = state.Current.HourType,
                AuthId = state.Current.AuthId,
                WorkDescription = state.Current.WorkDescription
            };
        }
    }

    private TimeSpan? GetPunchOutTimeSpan(PunchEditState state)
    {
        return state.Current.PunchOut?.ToLocalTime().TimeOfDay;
    }

    private void SetPunchOutTimeSpan(PunchEditState state, TimeSpan? value)
    {
        if (value.HasValue)
        {
            // Get the local date (use PunchOut if available, otherwise PunchIn)
            var localBaseDate = state.Current.PunchOut?.ToLocalTime().Date ?? state.Current.PunchIn.ToLocalTime().Date;
            var localDate = localBaseDate + value.Value;
            // Convert back to UTC for storage
            var utcDate = localDate.ToUniversalTime();
            state.Current = new PunchRecord
            {
                PunchId = state.Current.PunchId,
                PunchIn = state.Current.PunchIn,
                PunchOut = utcDate,
                HourType = state.Current.HourType,
                AuthId = state.Current.AuthId,
                WorkDescription = state.Current.WorkDescription
            };
        }
    }

    private void SetHourType(PunchEditState state, HourType value)
    {
        state.Current = new PunchRecord
        {
            PunchId = state.Current.PunchId,
            PunchIn = state.Current.PunchIn,
            PunchOut = state.Current.PunchOut,
            HourType = value,
            AuthId = state.Current.AuthId,
            WorkDescription = state.Current.WorkDescription
        };
    }

    public class PunchEditState
    {
        public PunchRecord Original { get; set; } = null!;
        public PunchRecord Current { get; set; } = null!;
        public bool IsEditing { get; set; }
        public Dictionary<string, string> ValidationErrors { get; set; } = new();

        public bool IsDirty =>
            Current.PunchIn != Original.PunchIn ||
            Current.PunchOut != Original.PunchOut ||
            Current.HourType != Original.HourType;
    }
}
