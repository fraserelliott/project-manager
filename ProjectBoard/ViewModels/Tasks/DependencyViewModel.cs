using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjectBoard.Models.Domain;
using TaskStatus = ProjectBoard.Models.Domain.TaskStatus;

namespace ProjectBoard.ViewModels.Tasks;

public sealed class DependencyViewModel : ObservableObject
{
    private static readonly Brush BlockingBrush = Hex("#EF4444");
    private static readonly Brush StaleBrush = Hex("#F59E0B");
    private readonly TaskItem _dep;
    private readonly TaskItem _task;

    public DependencyViewModel(TaskItem task, TaskItem dep)
    {
        _task = task;
        _dep = dep;
    }

    public Guid Id => _dep.Id;
    public string Name => _dep.Name;

    public string? Icon => IsBlocking() ? "\uE72E" : IsMakingStale() ? "\uE7BA" : null;

    public Brush Brush => IsBlocking() ? BlockingBrush : IsMakingStale() ? StaleBrush : Brushes.Transparent;

    private bool DependencyIncomplete()
    {
        return _dep.Status != TaskStatus.Completed;
    }

    private bool IsBlocking()
    {
        return DependencyIncomplete() && _task.Status != TaskStatus.Completed;
    }

    private bool IsMakingStale()
    {
        return DependencyIncomplete() && _task.Status == TaskStatus.Completed;
    }

    private static Brush Hex(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        return new SolidColorBrush(color);
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(Brush));
        OnPropertyChanged(nameof(Name));
    }
}