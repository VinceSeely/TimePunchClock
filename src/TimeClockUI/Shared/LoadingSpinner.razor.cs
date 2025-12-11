using Microsoft.AspNetCore.Components;
using TimeClockUI.Services;

namespace TimeClockUI.Shared;

public partial class LoadingSpinner : IAsyncDisposable
{
    [Inject] public LoadingService LoadingService { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LoadingService.Subscribe(StateHasChanged);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        LoadingService.Unsubscribe(StateHasChanged);
        await ValueTask.CompletedTask;
    }
}