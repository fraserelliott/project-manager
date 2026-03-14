using System.Windows.Threading;

namespace ProjectBoard.Utils;

public sealed class Debouncer
{
    private readonly DispatcherTimer _timer;
    private Action? _pendingAction;

    public Debouncer(int delayMs)
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(delayMs)
        };

        _timer.Tick += OnTimerTick;
    }

    public void Debounce(Action action)
    {
        _pendingAction = action;

        _timer.Stop();
        _timer.Start();
    }

    public void ExecuteNow()
    {
        _timer.Stop();

        if (_pendingAction is null)
            return;

        var action = _pendingAction;
        _pendingAction = null;

        action();
    }

    public void Cancel()
    {
        _timer.Stop();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timer.Stop();

        var action = _pendingAction;
        _pendingAction = null;

        action?.Invoke();
    }
}