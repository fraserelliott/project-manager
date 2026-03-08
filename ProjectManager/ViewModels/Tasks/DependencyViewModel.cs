using CommunityToolkit.Mvvm.ComponentModel;
using ProjectManager.Models.Domain;
using System.Windows.Media;
using TaskStatus = ProjectManager.Models.Domain.TaskStatus;

namespace ProjectManager.ViewModels.Tasks;

public sealed class DependencyViewModel : ObservableObject
{
    private readonly TaskItem _task;
    private readonly TaskItem _dep;

    private static readonly Brush BlockingBrush = Hex("#EF4444");
    private static readonly Brush StaleBrush = Hex("#F59E0B");

    public DependencyViewModel(TaskItem task, TaskItem dep)
    {
        _task = task;
        _dep = dep;
    }

    public Guid Id => _dep.Id;
    public string Name => _dep.Name;

    public string? Icon => IsBlocking() ? "\uE72E" : IsMakingStale() ? "\uE7BA" : null;

    public Brush Brush => IsBlocking() ? BlockingBrush : IsMakingStale() ? StaleBrush : Brushes.Transparent;

    private bool DependencyIncomplete() => _dep.Status != TaskStatus.Completed;

    private bool IsBlocking() => DependencyIncomplete() && _task.Status != TaskStatus.Completed;

    private bool IsMakingStale() => DependencyIncomplete() && _task.Status == TaskStatus.Completed;

    private static Brush Hex(string hex)
    {
        Color color = (Color)ColorConverter.ConvertFromString(hex);
        return new SolidColorBrush(color);
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(Brush));
        OnPropertyChanged(nameof(Name));
    }
}
