using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManager.Controls;
using ProjectManager.Models.Domain;
using ProjectManager.Services;
using ProjectManager.Stores;
using TaskStatus = ProjectManager.Models.Domain.TaskStatus;

namespace ProjectManager.ViewModels.Tasks;

public sealed class TaskItemViewModel : ObservableObject
{
    private readonly ProjectSession _session;
    private readonly TaskItem _task;
    private string? _draftName;
    private string? _draftPriority;
    private bool _editing;

    public TaskItemViewModel(ProjectSession session, TaskItem task, TasksViewModel owner)
    {
        _session = session;
        _task = task;
        Owner = owner;
        RestoreNameCommand = new RelayCommand(RestoreName);
        RestorePriorityCommand = new RelayCommand(RestorePriority);
        ConfirmDeleteTask = new RelayCommand(ConfirmDelete);
        RemoveTagCommand = new RelayCommand<Guid>(RemoveTag);
        RemoveDependencyCommand = new RelayCommand<Guid>(RemoveDependency);

        Dependencies = new ObservableCollection<DependencyViewModel>();
        foreach (var depId in task.DependencyIds)
        {
            var dep = _session.GetTask(depId);
            if (dep is not null)
                Dependencies.Add(new DependencyViewModel(task, dep));
        }
    }

    public ObservableCollection<DependencyViewModel> Dependencies { get; init; }
    public TasksViewModel Owner { get; }
    public IRelayCommand RestoreNameCommand { get; }
    public IRelayCommand RestorePriorityCommand { get; }
    public IRelayCommand ConfirmDeleteTask { get; }
    public IRelayCommand<Guid> AdvanceStatusCommand => Owner.AdvanceStatusCommand;
    public ICommand RemoveTagCommand { get; init; }
    public ICommand RemoveDependencyCommand { get; init; }
    public ICommand UpdateTagCommand => Owner.UpdateTagCommand;

    public IReadOnlyList<TagViewModel> Tags =>
        _task.TagIds
            .Select(id => Owner.GetTag(id))
            .Where(tag => tag is not null)
            .Cast<TagViewModel>()
            .ToList();

    public MarkdownViewMode MarkdownViewMode => IsEditing ? MarkdownViewMode.Raw : MarkdownViewMode.Rendered;

    public Guid Id => _task.Id;

    public string FormName
    {
        get => _draftName == null ? _task.Name : _draftName;
        set
        {
            var result = _session.RenameTask(Id, value);
            if (result.Success)
            {
                _draftName = null;
                NameErrorMessage = null;
                Owner.RefreshAll();
            }
            else
            {
                _draftName = value;
                NameErrorMessage = result.Message ?? "Rename was unsuccessful";
                OnPropertyChanged();
                OnPropertyChanged(nameof(NameErrorMessage));
                OnPropertyChanged(nameof(HasNameError));
            }
        }
    }

    public string Name => _task.Name;
    public bool IsRenameError => NameErrorMessage != null;
    public string? NameErrorMessage { get; private set; }

    public bool HasNameError => !string.IsNullOrWhiteSpace(NameErrorMessage);

    public string Description
    {
        get => _task.Description;
        set
        {
            var result = _session.UpdateDescriptionOnTask(Id, value);
            if (result.Success) OnPropertyChanged();
        }
    }

    public int Priority => _task.Priority;

    public string FormPriority
    {
        get => _draftPriority == null ? _task.Priority.ToString() : _draftPriority;
        set
        {
            if (int.TryParse(value, out var priority))
            {
                _session.UpdatePriorityOnTask(Id, priority);
                _draftPriority = null;
                HasPriorityError = false;
            }
            else
            {
                _draftPriority = value;
                HasPriorityError = true;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(Priority));
            OnPropertyChanged(nameof(HasPriorityError));
        }
    }

    public bool HasPriorityError { get; private set; }

    public TaskStatus Status => _task.Status;

    public bool IsBlocked => Status != TaskStatus.Completed && _session.IsTaskBlocked(Id);
    public bool IsStale => Status == TaskStatus.Completed && _session.IsTaskStale(Id);

    public string ButtonText =>
        Status == TaskStatus.NotStarted ? "Start" :
        Status == TaskStatus.Started ? "Complete" : "Reopen";

    public bool IsEditing
    {
        get => _editing;
        set
        {
            if (_editing == value) return;
            _editing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotEditing));
            OnPropertyChanged(nameof(MarkdownViewMode));
        }
    }

    public bool IsNotEditing => !IsEditing;

    private void RemoveTag(Guid tagId)
    {
        var result = _session.RemoveTagFromTask(Id, tagId);
        if (result.Success) OnPropertyChanged(nameof(Tags));
    }

    private void RemoveDependency(Guid dependencyId)
    {
        var result = _session.RemoveDependencyFromTask(Id, dependencyId);
        if (result.Success)
        {
            var vm = Dependencies.FirstOrDefault(d => d.Id == dependencyId);
            if (vm != null)
                Dependencies.Remove(vm);
            Owner.RefreshAll();
        }
    }

    private IReadOnlyList<AddDependencyOption> ComputeAvailableDependencies(string searchTerm)
    {
        List<AddDependencyOption> options = new();

        var search = searchTerm.Trim();

        foreach (var task in Owner.Tasks)
        {
            var alreadyOnTask = _task.DependencyIds.Contains(task.Id);
            if (alreadyOnTask || task.Id == Id)
                continue;

            if (!_session.WouldCreateCycle(Id, task.Id))
                if (search.Length == 0 || task.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    options.Add(new AddDependencyOption(task));
        }

        return options;
    }

    private void OpenCreateTagDialog(string name)
    {
        var result =
            new TagDialogService().PromptNewTag((name, color) => { return _session.AddTagToProject(name, color); },
                name);

        var tagId = Owner.TryAddTag(result);
        if (tagId == null) return;
        AddTag((Guid)tagId);
    }

    private void AddTag(Guid tagId)
    {
        var result = _session.AddTagToTask(Id, tagId);
        if (result.Success) OnPropertyChanged(nameof(Tags));
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Priority));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsBlocked));
        OnPropertyChanged(nameof(IsStale));
        OnPropertyChanged(nameof(ButtonText));
        OnPropertyChanged(nameof(Tags));
        OnPropertyChanged(nameof(HasNameError));
        OnPropertyChanged(nameof(NameErrorMessage));
        OnPropertyChanged(nameof(HasPriorityError));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Dependencies));

        foreach (var dependency in Dependencies) dependency.Refresh();
    }

    public void Reset()
    {
        RestoreName();
        RestorePriority();
        IsEditing = false;
    }

    private void RestoreName()
    {
        _draftName = null;
        NameErrorMessage = null;

        OnPropertyChanged(nameof(FormName));
        OnPropertyChanged(nameof(NameErrorMessage));
        OnPropertyChanged(nameof(HasNameError));
    }

    private void RestorePriority()
    {
        _draftPriority = null;
        HasPriorityError = false;

        OnPropertyChanged(nameof(FormPriority));
        OnPropertyChanged(nameof(HasPriorityError));
    }

    private void ConfirmDelete()
    {
        if (new ConfirmDialogService()
            .PromptConfirm("Are you sure you want to delete this task?", "Yes"))
            Owner.DeleteTask(Id);
    }
}