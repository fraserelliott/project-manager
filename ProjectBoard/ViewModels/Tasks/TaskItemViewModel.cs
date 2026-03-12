using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectBoard.Controls;
using ProjectBoard.Models.Domain;
using ProjectBoard.Services;
using ProjectBoard.Stores;
using TaskStatus = ProjectBoard.Models.Domain.TaskStatus;

namespace ProjectBoard.ViewModels.Tasks;

public sealed class TaskItemViewModel : ObservableObject
{
    private readonly ObservableCollection<AddDependencyOption> _availableDependencies = new();
    private readonly ObservableCollection<AddTagOption> _availableTagOptions = new();
    private readonly ProjectSession _session;
    private readonly TaskItem _task;
    private string _dependencySearchText = string.Empty;
    private string? _draftName;
    private string? _draftPriority;
    private bool _editing;
    private string _tagSearchText = string.Empty;

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
        AvailableTagOptions = new ReadOnlyObservableCollection<AddTagOption>(_availableTagOptions);
        AvailableDependencies = new ReadOnlyObservableCollection<AddDependencyOption>(_availableDependencies);

        Dependencies = new ObservableCollection<DependencyViewModel>();
        foreach (var depId in task.DependencyIds)
        {
            var dep = _session.GetTask(depId);
            if (dep is not null)
                Dependencies.Add(new DependencyViewModel(task, dep));
        }

        RefreshAvailableTags();
        RefreshAvailableDependencies();
    }

    public string TagSearchText
    {
        get => _tagSearchText;
        set
        {
            if (_tagSearchText == value)
                return;

            _tagSearchText = value;
            OnPropertyChanged();
            RefreshAvailableTags();
        }
    }

    public string DependencySearchText
    {
        get => _dependencySearchText;
        set
        {
            if (_dependencySearchText == value)
                return;

            _dependencySearchText = value;
            OnPropertyChanged();
            RefreshAvailableDependencies();
        }
    }

    public ReadOnlyObservableCollection<AddTagOption> AvailableTagOptions { get; }
    public ReadOnlyObservableCollection<AddDependencyOption> AvailableDependencies { get; }
    public bool HasAvailableDependencies => AvailableDependencies.Count > 0;
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

    public Func<object?, bool> TagOptionChosenHandler => HandleTagOptionChosen;

    public Func<object?, bool> DependencyOptionChosenHandler => HandleDependencyOptionChosen;

    private bool HandleTagOptionChosen(object? obj)
    {
        if (obj is not AddTagOption option) return false;
        switch (option)
        {
            case ExistingTagOption existing:
                AddTag(existing.Tag.Id);
                return true;

            case CreateTagOption create:
                var result = new TagDialogService().PromptNewTag(
                    (name, color) => _session.AddTagToProject(name, color),
                    create.Name);

                var tagId = Owner.TryAddTag(result);
                if (tagId == null) return false;
                AddTag(tagId.Value);
                return true;

            default:
                return false;
        }
    }

    private bool HandleDependencyOptionChosen(object? obj)
    {
        if (obj is not AddDependencyOption option) return false;
        var result = _session.AddDependencyToTask(Id, option.Task.Id);
        var dep = _session.GetTask(option.Task.Id);
        if (dep is null) return false;
        Dependencies.Add(new DependencyViewModel(_task, dep));
        Owner.RefreshAll();
        return true;
    }

    private void RefreshAvailableDependencies()
    {
        _availableDependencies.Clear();

        var search = DependencySearchText.Trim();

        foreach (var task in Owner.Tasks)
        {
            var alreadyOnTask = _task.DependencyIds.Contains(task.Id);
            if (alreadyOnTask || task.Id == Id)
                continue;

            if (!_session.WouldCreateCycle(Id, task.Id))
                if (search.Length == 0 || task.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    _availableDependencies.Add(new AddDependencyOption(task));
        }
    }

    public void RefreshAvailableTags()
    {
        _availableTagOptions.Clear();

        var search = TagSearchText.Trim();

        foreach (var option in Owner.AllTagOptions)
        {
            if (_task.TagIds.Contains(option.Tag.Id))
                continue;

            if (search.Length == 0 || option.Tag.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                _availableTagOptions.Add(option);
        }

        var exactMatchExists = _availableTagOptions
            .Any(x => x is ExistingTagOption existing &&
                      string.Equals(existing.Tag.Name, search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search) && !exactMatchExists && !_session.HasTagWithName(search))
            _availableTagOptions.Add(new CreateTagOption(search));
    }

    private void RemoveTag(Guid tagId)
    {
        var result = _session.RemoveTagFromTask(Id, tagId);
        if (result.Success) OnPropertyChanged(nameof(Tags));
        RefreshAvailableTags();
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

    private void AddTag(Guid tagId)
    {
        var result = _session.AddTagToTask(Id, tagId);
        if (result.Success) OnPropertyChanged(nameof(Tags));
        RefreshAvailableTags();
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
        RefreshAvailableTags();
        RefreshAvailableDependencies();

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