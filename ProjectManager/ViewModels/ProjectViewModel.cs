using CommunityToolkit.Mvvm.Input;
using ProjectManager.Models.Domain;
using System.Collections.ObjectModel;

namespace ProjectManager.ViewModels;

public sealed class ProjectViewModel : ViewModelBase
{
    private readonly Project _project;
    public IRelayCommand<Guid> AdvanceStatusCommand { get; }

    public ProjectViewModel(Project project)
    {
        _project = project;

        Tasks = new ObservableCollection<TaskItemViewModel>(
            _project.Tasks.Select(t => new TaskItemViewModel(_project, t))
        );

        AdvanceStatusCommand = new RelayCommand<Guid>(execute: AdvanceStatus, canExecute: id => !_project.IsBlocked(id));
    }

    private void AdvanceStatus(Guid id)
    {
        _project.AdvanceStatus(id);
        RefreshAll();
    }

    public string ProjectName => _project.Name;

    public ObservableCollection<TaskItemViewModel> Tasks { get; }

    private TaskItemViewModel? _selectedTask;
    public TaskItemViewModel? SelectedTask
    {
        get => _selectedTask;
        set => SetField(ref _selectedTask, value);
    }

    public void RefreshAll()
    {
        OnPropertyChanged(nameof(ProjectName));
        AdvanceStatusCommand.NotifyCanExecuteChanged();
        foreach (var t in Tasks) t.Refresh();
    }
}