using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectBoard.Services;
using ProjectBoard.Stores;
using ProjectBoard.Views;

namespace ProjectBoard.ViewModels.Tasks;

public record RemoveTagRequest(Guid TaskId, Guid TagId);

public sealed class TasksViewModel : ObservableObject
{
    private readonly ObservableCollection<ExistingTagOption> _allTagOptions = new();
    private readonly Dictionary<Guid, TaskDetailsWindow> _openTaskWindows = new();
    private readonly ProjectSession _session;
    private readonly Dictionary<Guid, TagViewModel> _tags = new();
    private readonly Dictionary<Guid, TaskItemViewModel> _tasksById;

    private TaskItemViewModel? _selectedTask;

    public TasksViewModel(ProjectSession session)
    {
        _session = session;
        Tasks = new ObservableCollection<TaskItemViewModel>();
        _tasksById = new Dictionary<Guid, TaskItemViewModel>();
        AllTagOptions = new ReadOnlyObservableCollection<ExistingTagOption>(_allTagOptions);

        foreach (var tag in _session.Project.Tags)
        {
            var vm = new TagViewModel(tag);
            _tags[tag.Id] = vm;
            _allTagOptions.Add(new ExistingTagOption(vm));
        }

        foreach (var task in _session.Project.Tasks)
        {
            var vm = new TaskItemViewModel(_session, task, this);

            Tasks.Add(vm);
            _tasksById[vm.Id] = vm;
        }

        NewTaskCommand = new RelayCommand(HandleNewTask);
        AdvanceStatusCommand = new RelayCommand<Guid>(AdvanceStatus, id => !_session.IsTaskBlocked(id));
        ShowDetailsCommand = new RelayCommand<Guid>(ShowDetails);
        UpdateTagCommand = new RelayCommand<Guid>(HandleUpdateTag);
        RemoveTagCommand = new RelayCommand<RemoveTagRequest>(HandleRemoveTag);
        DeleteTagCommand = new RelayCommand<Guid>(HandleDeleteTag);
    }

    public RelayCommand<Guid> AdvanceStatusCommand { get; }
    public RelayCommand<Guid> ShowDetailsCommand { get; }

    public ObservableCollection<TaskItemViewModel> Tasks { get; }
    public bool HasTasks => Tasks.Count > 0;
    public ReadOnlyObservableCollection<ExistingTagOption> AllTagOptions { get; }

    public TaskItemViewModel? SelectedTask
    {
        get => _selectedTask;
        set => SetProperty(ref _selectedTask, value);
    }

    public RelayCommand NewTaskCommand { get; }
    public RelayCommand<Guid> UpdateTagCommand { get; }
    public RelayCommand<RemoveTagRequest> RemoveTagCommand { get; }
    public RelayCommand<Guid> DeleteTagCommand { get; }

    private void Notify(OperationResult result)
    {
        if (!result.Success)
            return;

        switch (result.ResultAction)
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

    private void HandleNewTask()
    {
        var result =
            new PromptService().PromptForString("Add Task", "Task name", "Add", name => _session.AddTask(name));

        if (result is not { Success: true })
            return;

        if (result.ResultAction is RefreshTask r)
        {
            var task = _session.GetTask(r.TaskId);
            var vm = new TaskItemViewModel(_session, task, this);
            Tasks.Add(vm);
            _tasksById.Add(task.Id, vm);
            RefreshAll();
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

    private void HandleUpdateTag(Guid tagId)
    {
        if (!_tags.TryGetValue(tagId, out var vm))
            return;

        var result =
            new TagDialogService().PromptTagUpdate((newName, newColor) => _session.UpdateTag(tagId, newName, newColor),
                vm.Name, vm.Color);
        if (result is null) return;
        Notify(result);
    }

    private void HandleRemoveTag(RemoveTagRequest request)
    {
        if (!_tasksById.TryGetValue(request.TaskId, out var taskVm))
            return;

        if (taskVm.RemoveTagCommand.CanExecute(request.TagId))
            taskVm.RemoveTagCommand.Execute(request.TagId);
    }

    private void HandleDeleteTag(Guid tagId)
    {
        if (!_tags.ContainsKey(tagId))
            return;

        if (!new ConfirmDialogService().PromptConfirm(
                "This will delete the tag and remove it from all tasks in the project. Continue?", "Yes")) return;

        var result = _session.DeleteTagFromProject(tagId);
        if (!result.Success) return;

        _tags.Remove(tagId);
        var option = _allTagOptions.FirstOrDefault(o => o.Tag.Id == tagId);
        if (option != null)
            _allTagOptions.Remove(option);

        Notify(result);
    }

    public void RefreshAll()
    {
        AdvanceStatusCommand.NotifyCanExecuteChanged();
        foreach (var t in Tasks) t.Refresh();
    }

    public TaskItemViewModel? GetTaskItemVM(Guid taskId)
    {
        return _tasksById.TryGetValue(taskId, out var task)
            ? task
            : null;
    }

    public void DeleteTask(Guid taskId)
    {
        var result = _session.RemoveTask(taskId);

        if (!result.Success) return;

        if (_openTaskWindows.TryGetValue(taskId, out var window)) window.Close();

        if (_tasksById.TryGetValue(taskId, out var task))
        {
            Tasks.Remove(task);
            _tasksById.Remove(taskId);
        }
    }

    public TagViewModel? GetTag(Guid id)
    {
        return _tags.TryGetValue(id, out var tag) ? tag : null;
    }

    public Guid? TryAddTag(OperationResult? result)
    {
        if (result is not null && result.Success && result.ResultAction is RefreshTag refreshTag)
        {
            var tag = _session.GetTag(refreshTag.TagId);
            if (tag is null) return null;
            if (_tags.ContainsKey(refreshTag.TagId)) return null;
            var vm = new TagViewModel(tag);
            _tags.Add(refreshTag.TagId, vm);
            _allTagOptions.Add(new ExistingTagOption(vm));
            Notify(result);
            return refreshTag.TagId;
        }

        return null;
    }

    public IReadOnlyList<TagViewModel> GetAllTags()
    {
        return _tags.Values.ToList();
    }
}