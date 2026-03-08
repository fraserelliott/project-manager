using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManager.Services;
using ProjectManager.Stores;
using ProjectManager.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ProjectManager.ViewModels.Tasks;

public sealed class TasksViewModel : ObservableObject
{
    private readonly ProjectSession _session;
    public IRelayCommand<Guid> AdvanceStatusCommand { get; }
    public IRelayCommand<Guid> ShowDetailsCommand { get; }

    public string ProjectName => _session.Project.Name;

    public ObservableCollection<TaskItemViewModel> Tasks { get; }
    private readonly Dictionary<Guid, TaskItemViewModel> _tasksById;
    private readonly Dictionary<Guid, TaskDetailsWindow> _openTaskWindows = new();

    private TaskItemViewModel? _selectedTask;
    public TaskItemViewModel? SelectedTask
    {
        get => _selectedTask;
        set => SetProperty(ref _selectedTask, value);
    }

    public ICommand NewTaskCommand { get; }

    private readonly Dictionary<Guid, TagViewModel> _tags = new();

    public TasksViewModel(ProjectSession session, PromptService promptService)
    {
        _session = session;
        Tasks = new ObservableCollection<TaskItemViewModel>();
        _tasksById = new Dictionary<Guid, TaskItemViewModel>();

        foreach (var task in _session.Project.Tasks)
        {
            var vm = new TaskItemViewModel(_session, task, this);

            Tasks.Add(vm);
            _tasksById[vm.Id] = vm;
        }

        foreach (var tag in _session.Project.Tags)
        {
            var vm = new TagViewModel(tag);
            _tags[tag.Id] = vm;
        }

        NewTaskCommand = new RelayCommand(() =>
        {
            var result = promptService.PromptForString("Add task", "Task name", "Add", name => session.AddTask(name));

            if (result is not { Success: true })
                return;

            if (result.Refresh is RefreshTask r)
            {
                var task = session.GetTask(r.TaskId);
                var vm = new TaskItemViewModel(session, task, this);
                Tasks.Add(vm);
                _tasksById.Add(task.Id, vm);
                RefreshAll();
            }
        });

        AdvanceStatusCommand = new RelayCommand<Guid>(execute: AdvanceStatus, canExecute: id => !_session.IsTaskBlocked(id));
        ShowDetailsCommand = new RelayCommand<Guid>(execute: ShowDetails);
    }

    private void Notify(OperationResult result)
    {
        if (!result.Success)
            return;

        switch (result.Refresh)
        {
            case RefreshNone:
                break;

            case RefreshProject:
                RefreshAll();
                break;

            case RefreshTask(var taskId):
                GetTaskItemVM(taskId)?.Refresh();
                break;

            case RefreshTag(var tagId):
                RefreshAll();
                break;
        }
    }

    private void AdvanceStatus(Guid id)
    {
        var result = _session.AdvanceStatus(id);
        Notify(result);
    }

    private void ShowDetails(Guid id)
    {
        if (!_tasksById.TryGetValue(id, out var vm))
            return;

        if (_openTaskWindows.TryGetValue(id, out var existingWindow))
        {
            if (existingWindow.WindowState == WindowState.Minimized)
                existingWindow.WindowState = WindowState.Normal;

            existingWindow.Activate();
            existingWindow.Focus();
            return;
        }

        var window = new TaskDetailsWindow
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        window.Closed += (_, _) =>
        {
            _openTaskWindows.Remove(id);
            vm.Reset();
        };
        _openTaskWindows[id] = window;
        window.Show();
    }

    public void RefreshAll()
    {
        OnPropertyChanged(nameof(ProjectName));
        AdvanceStatusCommand.NotifyCanExecuteChanged();
        foreach (var t in Tasks) t.Refresh();
    }

    public TaskItemViewModel? GetTaskItemVM(Guid taskId) =>
        _tasksById.TryGetValue(taskId, out var task)
                ? task
                : null;

    public void DeleteTask(Guid taskId)
    {
        OperationResult result = _session.RemoveTask(taskId);

        if (!result.Success)
        {
            return;
        }

        if (_openTaskWindows.TryGetValue(taskId, out var window))
        {
            window.Close();
        }

        if (_tasksById.TryGetValue(taskId, out var task))
        {
            Tasks.Remove(task);
            _tasksById.Remove(taskId);
        }
    }

    public TagViewModel? GetTag(Guid id) => _tags.TryGetValue(id, out var tag) ? tag : null;

    public void TryAddTag(OperationResult? result)
    {
        if (result is not null && result.Success && result.Refresh is RefreshTag refreshTag)
        {
            var tag = _session.GetTag(refreshTag.TagId);
            if (tag is null) return;
            if (_tags.ContainsKey(refreshTag.TagId)) return;
            _tags.Add(refreshTag.TagId, new TagViewModel(tag));
            Notify(result);
        }
    }

    public IReadOnlyList<TagViewModel> GetAllTags() => _tags.Values.ToList();
}