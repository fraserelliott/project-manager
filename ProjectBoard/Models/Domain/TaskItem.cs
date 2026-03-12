namespace ProjectBoard.Models.Domain;

public sealed class TaskItem
{
    private readonly List<Guid> _dependencyIds;

    private readonly List<Guid> _tagIds;

    public TaskItem(Guid id, string name, string description,
        IEnumerable<Guid> tagIds,
        IEnumerable<Guid> dependencyIds,
        TaskStatus status,
        int priority)
    {
        Id = id;
        Status = status;
        Rename(name);
        SetDescription(description);
        SetPriority(priority);

        _tagIds = tagIds?.Distinct().ToList() ?? new List<Guid>();
        _dependencyIds = dependencyIds?.Distinct().ToList() ?? new List<Guid>();
    }

    public TaskItem(Guid id, string name) : this(id, name, string.Empty, Array.Empty<Guid>(), Array.Empty<Guid>(),
        TaskStatus.NotStarted, 0)
    {
    }

    public Guid Id { get; init; }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Priority { get; private set; }
    public IReadOnlyList<Guid> TagIds => _tagIds;
    public IReadOnlyList<Guid> DependencyIds => _dependencyIds;

    public TaskStatus Status { get; private set; } = TaskStatus.NotStarted;

    public void Rename(string newName)
    {
        newName = (newName ?? "").Trim();
        if (newName.Length == 0)
            throw new ArgumentException("Task name is required.", nameof(newName));

        Name = newName;
    }

    public void SetDescription(string description)
    {
        Description = description ?? "";
    }

    public void SetPriority(int priority)
    {
        if (priority < 0)
            throw new ArgumentOutOfRangeException(nameof(priority));

        Priority = priority;
    }

    public bool AddTag(Guid tagId)
    {
        if (_tagIds.Contains(tagId)) return false;
        _tagIds.Add(tagId);
        return true;
    }

    public bool RemoveTag(Guid tagId)
    {
        return _tagIds.Remove(tagId);
    }

    public void ClearTags()
    {
        _tagIds.Clear();
    }

    public bool AddDependency(Guid dependencyId)
    {
        if (dependencyId == Id)
            throw new InvalidOperationException("Task cannot depend on itself.");

        if (_dependencyIds.Contains(dependencyId)) return false;

        _dependencyIds.Add(dependencyId);
        return true;
    }

    public bool RemoveDependency(Guid dependencyId)
    {
        return _dependencyIds.Remove(dependencyId);
    }

    public void ClearDependencies()
    {
        _dependencyIds.Clear();
    }

    internal void Start()
    {
        ChangeStatus(TaskStatus.Started);
    }

    internal void Complete()
    {
        ChangeStatus(TaskStatus.Completed);
    }

    private void ChangeStatus(TaskStatus next)
    {
        if (Status == next) return;

        if (!IsAllowedTransition(Status, next))
            throw new InvalidOperationException(
                $"Invalid status change: {Status} → {next}. Allowed: start, complete, reopen.");

        Status = next;
    }

    private static bool IsAllowedTransition(TaskStatus from, TaskStatus to)
    {
        return (from, to) switch
        {
            (TaskStatus.NotStarted, TaskStatus.Started) => true,
            (TaskStatus.Started, TaskStatus.Completed) => true,
            (TaskStatus.Completed, TaskStatus.Started) => true,
            _ => false
        };
    }

    public TaskItemData ToData()
    {
        return new TaskItemData
        {
            Id = Id,
            Name = Name,
            Description = Description,
            TagIds = TagIds.ToList(),
            DependencyIds = DependencyIds.ToList(),
            Priority = Priority,
            Status = Status
        };
    }

    public static TaskItem FromData(TaskItemData data)
    {
        return new TaskItem(
            data.Id,
            data.Name,
            data.Description,
            data.TagIds,
            data.DependencyIds,
            data.Status,
            data.Priority
        );
    }
}