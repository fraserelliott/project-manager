using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManager.Models.Domain;
using ProjectManager.Services;
using ProjectManager.Stores;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TaskStatus = ProjectManager.Models.Domain.TaskStatus;

namespace ProjectManager.ViewModels.Tasks;

public sealed class TaskItemViewModel : ObservableObject
{
    private readonly ProjectSession _session;
    private readonly TaskItem _task;
    private string? _draftName;
    private string? _nameErrorMessage;
    private string? _draftPriority;
    private bool _hasPriorityError = false;
    private string _tagSearchText = "";
    private string _dependencySearchText = "";
    private AddTagOption? _selectedTagOption;
    private AddDependencyOption? _selectedDependencyOption;
    public ObservableCollection<DependencyViewModel> Dependencies { get; init; }
    public TasksViewModel Owner { get; }
    public IRelayCommand RestoreNameCommand { get; }
    public IRelayCommand RestorePriorityCommand { get; }
    public IRelayCommand ConfirmDeleteTask { get; }
    public IRelayCommand<Guid> AdvanceStatusCommand => Owner.AdvanceStatusCommand;
    public ICommand RemoveTagCommand { get; init; }

    public IReadOnlyList<TagViewModel> Tags =>
    _task.TagIds
        .Select(id => Owner.GetTag(id))
        .Where(tag => tag is not null)
        .Cast<TagViewModel>()
        .ToList();

    public TaskItemViewModel(ProjectSession session, TaskItem task, TasksViewModel owner)
    {
        _session = session;
        _task = task;
        Owner = owner;
        RestoreNameCommand = new RelayCommand(RestoreName);
        RestorePriorityCommand = new RelayCommand(RestorePriority);
        ConfirmDeleteTask = new RelayCommand(ConfirmDelete);
        RemoveTagCommand = new RelayCommand<Guid>(RemoveTag);

        Dependencies = new ObservableCollection<DependencyViewModel>();
        foreach (Guid depId in task.DependencyIds)
        {
            TaskItem? dep = _session.GetTask(depId);
            if (dep is not null)
                Dependencies.Add(new DependencyViewModel(task, dep));
        }
    }

    private void RemoveTag(Guid tagId)
    {
        if (!_task.TagIds.Contains(tagId))
            return;

        _task.RemoveTag(tagId);
        OnPropertyChanged(nameof(Tags));
        OnPropertyChanged(nameof(AvailableTagOptions));
    }

    public string TagSearchText
    {
        get => _tagSearchText;
        set
        {
            if (SetProperty(ref _tagSearchText, value))
            {
                OnPropertyChanged(nameof(AvailableTagOptions));
            }
        }
    }

    public string DependencySearchText
    {
        get => _dependencySearchText;
        set
        {
            if (SetProperty(ref _dependencySearchText, value))
            {
                OnPropertyChanged(nameof(AvailableDependencyOptions));
            }
        }
    }

    public AddTagOption? SelectedTagOption
    {
        get => _selectedTagOption;
        set
        {
            if (SetProperty(ref _selectedTagOption, value) && value is not null)
            {
                HandleTagOptionSelected(value);
            }
        }
    }

    public AddDependencyOption? SelectedDependencyOption
    {
        get => _selectedDependencyOption;
        set
        {
            if (SetProperty(ref _selectedDependencyOption, value) && value is not null)
            {
                HandleDependencyOptionSelected(value);
            }
        }
    }

    public IReadOnlyList<AddTagOption> AvailableTagOptions
    {
        get
        {
            List<AddTagOption> options = new();

            string search = TagSearchText.Trim();

            foreach (TagViewModel tag in Owner.GetAllTags())
            {
                bool alreadyOnTask = _task.TagIds.Contains(tag.Id);
                if (alreadyOnTask)
                    continue;

                if (search.Length == 0 || tag.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    options.Add(new ExistingTagOption(tag));
                }
            }

            bool exactMatchExists = options
                .OfType<ExistingTagOption>()
                .Any(x => string.Equals(x.Tag.Name, search, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(search) && !exactMatchExists)
            {
                options.Add(new CreateTagOption(search));
            }

            return options;
        }
    }

    public bool HasAvailableDependencies => ComputeAvailableDependencies("").Count > 0;

    public IReadOnlyList<AddDependencyOption> AvailableDependencyOptions => ComputeAvailableDependencies(DependencySearchText);

    private IReadOnlyList<AddDependencyOption> ComputeAvailableDependencies(string searchTerm)
    {
        List<AddDependencyOption> options = new();

        string search = searchTerm.Trim();

        foreach (TaskItemViewModel task in Owner.Tasks)
        {
            bool alreadyOnTask = _task.DependencyIds.Contains(task.Id);
            if (alreadyOnTask || task.Id == Id)
                continue;

            if (!_session.WouldCreateCycle(Id, task.Id))
            {
                if (search.Length == 0 || task.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    options.Add(new AddDependencyOption(task));
            }
        }

        return options;
    }

    private void HandleTagOptionSelected(AddTagOption option)
    {
        switch (option)
        {
            case ExistingTagOption existing:
                AddTag(existing.Tag.Id);
                ResetTagPicker();
                break;

            case CreateTagOption create:
                OpenCreateTagDialog(create.Name);
                ResetTagPicker();
                break;
        }
    }

    private void HandleDependencyOptionSelected(AddDependencyOption option)
    {
        if (!_task.AddDependency(option.Task.Id)) return;
        var dep = _session.GetTask(option.Task.Id);
        if (dep is null) return;
        Dependencies.Add(new DependencyViewModel(_task, dep));
        Owner.RefreshAll();
    }

    private void OpenCreateTagDialog(string name)
    {
        var result = new TagDialogService().PromptNewTag((name, color) =>
        {
            return _session.AddTag(_task.Id, name, color);
        }, name);

        Owner.TryAddTag(result);
    }

    private void AddTag(Guid tagId)
    {
        _task.AddTag(tagId);
        OnPropertyChanged(nameof(Tags));
    }

    private void ResetTagPicker()
    {
        _selectedTagOption = null;
        _tagSearchText = "";
        OnPropertyChanged(nameof(SelectedTagOption));
        OnPropertyChanged(nameof(TagSearchText));
        OnPropertyChanged(nameof(AvailableTagOptions));
    }

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
                _nameErrorMessage = null;
                Owner.RefreshAll();
            }
            else
            {
                _draftName = value;
                _nameErrorMessage = result.Message ?? "Rename was unsuccessful";
                OnPropertyChanged();
                OnPropertyChanged(nameof(NameErrorMessage));
                OnPropertyChanged(nameof(HasNameError));
            }
        }
    }

    public string Name => _task.Name;
    public bool IsRenameError => _nameErrorMessage != null;
    public string? NameErrorMessage => _nameErrorMessage;
    public bool HasNameError => !string.IsNullOrWhiteSpace(_nameErrorMessage);

    public string Description => _task.Description;
    public int Priority => _task.Priority;
    public string FormPriority
    {
        get => _draftPriority == null ? _task.Priority.ToString() : _draftPriority;
        set
        {
            if (int.TryParse(value, out var priority))
            {
                _task.SetPriority(priority);
                _draftPriority = null;
                _hasPriorityError = false;
            }
            else
            {
                _draftPriority = value;
                _hasPriorityError = true;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(Priority));
            OnPropertyChanged(nameof(HasPriorityError));
        }
    }
    public bool HasPriorityError => _hasPriorityError;

    public TaskStatus Status => _task.Status;

    public bool IsBlocked => Status != TaskStatus.Completed && _session.IsTaskBlocked(Id);
    public bool IsStale => Status == TaskStatus.Completed && _session.IsTaskStale(Id);

    public string ButtonText =>
        Status == TaskStatus.NotStarted ? "Start" :
        Status == TaskStatus.Started ? "Complete" : "Reopen";

    private bool _editing;
    public bool IsEditing
    {
        get => _editing;
        set
        {
            if (_editing == value) return;
            _editing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotEditing));
        }
    }
    public bool IsNotEditing => !IsEditing;

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
        OnPropertyChanged(nameof(AvailableTagOptions));
        OnPropertyChanged(nameof(AvailableDependencyOptions));
        OnPropertyChanged(nameof(HasAvailableDependencies));

        foreach (var dependency in Dependencies)
        {
            dependency.Refresh();
        }
    }

    public void Reset()
    {
        RestoreName();
        RestorePriority();
        IsEditing = false;
    }

    public void RestoreName()
    {
        _draftName = null;
        _nameErrorMessage = null;

        OnPropertyChanged(nameof(FormName));
        OnPropertyChanged(nameof(NameErrorMessage));
        OnPropertyChanged(nameof(HasNameError));
    }

    public void RestorePriority()
    {
        _draftPriority = null;
        _hasPriorityError = false;

        OnPropertyChanged(nameof(FormPriority));
        OnPropertyChanged(nameof(HasPriorityError));
    }

    private void ConfirmDelete()
    {
        if (new ConfirmDialogService()
        .PromptConfirm("Are you sure you want to delete this task?", "Yes"))
        {
            Owner.DeleteTask(Id);
        }
    }
}