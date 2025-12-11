namespace TimeClockUI.Services;

public class LoadingService
{
    private int _loadingCount = 0;
    private event Action? OnLoadingStateChanged;

    public bool IsLoading => _loadingCount > 0;

    public void Subscribe(Action callback)
    {
        OnLoadingStateChanged += callback;
    }

    public void Unsubscribe(Action callback)
    {
        OnLoadingStateChanged -= callback;
    }

    public void StartLoading()
    {
        _loadingCount++;
        NotifyStateChanged();
    }

    public void StopLoading()
    {
        if (_loadingCount > 0)
        {
            _loadingCount--;
            NotifyStateChanged();
        }
    }

    public IDisposable Track()
    {
        StartLoading();
        return new LoadingTracker(this);
    }

    private void NotifyStateChanged()
    {
        OnLoadingStateChanged?.Invoke();
    }

    private class LoadingTracker : IDisposable
    {
        private readonly LoadingService _service;
        private bool _disposed = false;

        public LoadingTracker(LoadingService service)
        {
            _service = service;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _service.StopLoading();
                _disposed = true;
            }
        }
    }
}
