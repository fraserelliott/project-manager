namespace ProjectManager.Models.Domain
{
    public sealed class TaskItem
    {
        public Guid Id { get; init; }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public int Priority { get; private set; }

        private readonly List<Guid> _tagIds;
        public IReadOnlyList<Guid> TagIds => _tagIds;

        private readonly List<Guid> _dependencyIds;
        public IReadOnlyList<Guid> DependencyIds => _dependencyIds;

        public TaskStatus Status { get; private set; } = TaskStatus.NotStarted;

        public TaskItem(Guid id, string name, string description,
               IEnumerable<Guid> tagIds,
               IEnumerable<Guid> dependencyIds,
               int priority)
        {
            Id = id;
            Rename(name);
            SetDescription(description);
            SetPriority(priority);

            _tagIds = tagIds?.Distinct().ToList() ?? new();
            _dependencyIds = dependencyIds?.Distinct().ToList() ?? new();
        }

        public void Rename(string newName)
        {
            newName = (newName ?? "").Trim();
            if (newName.Length == 0)
                throw new ArgumentException("Task name is required.", nameof(newName));

            Name = newName;
        }

        public void SetDescription(string description)
        {
            Description = description?.Trim() ?? "";
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

        public bool RemoveTag(Guid tagId) => _tagIds.Remove(tagId);

        public void ClearTags() => _tagIds.Clear();

        public bool AddDependency(Guid dependencyId)
        {
            if (dependencyId == Id)
                throw new InvalidOperationException("Task cannot depend on itself.");

            if (_dependencyIds.Contains(dependencyId)) return false;

            _dependencyIds.Add(dependencyId);
            return true;
        }

        public bool RemoveDependency(Guid dependencyId) => _dependencyIds.Remove(dependencyId);

        public void ClearDependencies() => _dependencyIds.Clear();

        internal void Start() => ChangeStatus(TaskStatus.Started);

        internal void Complete() => ChangeStatus(TaskStatus.Completed);

        private void ChangeStatus(TaskStatus next)
        {
            if (Status == next) return;

            if (!IsAllowedTransition(Status, next))
                throw new InvalidOperationException($"Invalid status change: {Status} → {next}. Allowed: start, complete, reopen.");

            Status = next;
        }

        private static bool IsAllowedTransition(TaskStatus from, TaskStatus to) =>
            (from, to) switch
            {
                (TaskStatus.NotStarted, TaskStatus.Started) => true,
                (TaskStatus.Started, TaskStatus.Completed) => true,
                (TaskStatus.Completed, TaskStatus.Started) => true,
                _ => false
            };
    }


}
