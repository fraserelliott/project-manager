using ProjectManager.Models.Domain;
using TaskStatus = ProjectManager.Models.Domain.TaskStatus;

namespace ProjectManager.ViewModels;

public sealed class TaskItemViewModel : ViewModelBase
{
    private readonly Project _project;
    private readonly TaskItem _task;

    public TaskItemViewModel(Project project, TaskItem task)
    {
        _project = project;
        _task = task;
    }

    public Guid Id => _task.Id;
    public string Name => _task.Name;
    public string Description => _task.Description;
    public int Priority => _task.Priority;
    public TaskStatus Status => _task.Status;

    public bool IsBlocked => Status != TaskStatus.Completed && _project.IsBlocked(Id);
    public bool IsStale => Status == TaskStatus.Completed && _project.IsStale(Id);

    public string ButtonText =>
        Status == TaskStatus.NotStarted ? "Start" :
        Status == TaskStatus.Started ? "Complete" : "Reopen";

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Priority));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsBlocked));
        OnPropertyChanged(nameof(IsStale));
        OnPropertyChanged(nameof(ButtonText));
    }
}