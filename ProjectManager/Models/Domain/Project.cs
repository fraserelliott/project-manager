namespace ProjectManager.Models.Domain
{
    public sealed class Project
    {
        public string Name { get; private set; } = "";
        private readonly List<TaskItem> _tasks = new();
        private readonly Dictionary<Guid, TaskItem> _tasksById = new();
        public IReadOnlyList<TaskItem> Tasks => _tasks;
        private readonly List<Note> _notes = new();
        public IReadOnlyList<Note> Notes => _notes;

        public Project(string name)
        {
            Rename(name);
        }

        public void Rename(string newName)
        {
            newName = (newName ?? "").Trim();
            if (newName.Length == 0)
                throw new ArgumentException("Project name is required.", nameof(newName));

            Name = newName;
        }

        public TaskItem AddTask(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0)
                throw new ArgumentException("Task name is required.", nameof(name));

            if (HasTaskWithName(name))
                throw new ArgumentException("Task name already exists.", nameof(name));

            var task = new TaskItem(
                id: Guid.NewGuid(),
                name: name,
                description: "",
                tagIds: Array.Empty<Guid>(),
                dependencyIds: Array.Empty<Guid>(),
                priority: 0
            );

            _tasks.Add(task);
            _tasksById.Add(task.Id, task);
            return task;
        }

        public Note AddNote(string name, string markdown = "")
        {
            name = (name ?? "").Trim();
            if (name.Length == 0)
                throw new ArgumentException("Note name is required.", nameof(name));

            if (HasNoteWithName(name))
                throw new ArgumentException("Note name already exists.", nameof(name));

            var note = new Note(
                id: Guid.NewGuid(),
                name: name,
                markdown: markdown
                );

            _notes.Add(note);
            return note;
        }

        public void RenameTask(Guid taskId, string newName)
        {
            newName = (newName ?? "").Trim();
            if (newName.Length == 0)
                throw new ArgumentException("Task name is required.", nameof(newName));

            var task = GetTask(taskId);

            // Allow "no-op rename"
            if (string.Equals(task.Name, newName, StringComparison.OrdinalIgnoreCase))
                return;

            if (_tasks.Any(t => t.Id != taskId &&
                            string.Equals(t.Name, newName, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Task name already exists.", nameof(newName));

            task.Rename(newName);
        }

        public void RenameNote(Guid noteId, string newName)
        {
            newName = (newName ?? "").Trim();
            if (newName.Length == 0)
                throw new ArgumentException("Note name is required.", nameof(newName));

            var note = GetNote(noteId);

            // Allow "no-op rename"
            if (string.Equals(note.Name, newName, StringComparison.OrdinalIgnoreCase))
                return;

            if (_notes.Any(n => n.Id != noteId &&
                            string.Equals(n.Name, newName, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Note name already exists.", nameof(newName));

            note.Rename(newName);
        }

        public void RemoveTask(Guid taskId)
        {
            if (!_tasksById.Remove(taskId))
                throw new KeyNotFoundException("Task not found.");

            var idx = _tasks.FindIndex(t => t.Id == taskId);
            if (idx >= 0) _tasks.RemoveAt(idx);

            // Remove this task as a dependency from all remaining tasks
            foreach (var task in _tasks)
            {
                task.RemoveDependency(taskId);
            }
        }


        public void RemoveNote(Guid noteId)
        {
            var idx = _notes.FindIndex(n => n.Id == noteId);
            if (idx < 0) throw new KeyNotFoundException("Note not found.");
            _notes.RemoveAt(idx);
        }

        public TaskItem GetTask(Guid id) =>
            _tasksById.TryGetValue(id, out var task)
                ? task
                : throw new KeyNotFoundException("Task not found.");

        public Note GetNote(Guid id) => _notes.FirstOrDefault(n => n.Id == id) ?? throw new KeyNotFoundException("Note not found.");

        public bool HasTaskWithName(string name)
        {
            name = (name ?? "").Trim();
            return _tasks.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasNoteWithName(string name)
        {
            name = (name ?? "").Trim();
            return _notes.Any(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public void StartTask(Guid taskId)
        {
            if (IsBlocked(taskId))
                throw new InvalidOperationException("Task is blocked by incomplete dependencies.");

            GetTask(taskId).Start();
        }

        public void CompleteTask(Guid taskId)
        {
            if (IsBlocked(taskId))
                throw new InvalidOperationException("Task is blocked by incomplete dependencies.");

            GetTask(taskId).Complete();
        }

        public bool IsBlocked(Guid taskId)
        {
            var cache = new Dictionary<Guid, bool>();
            return IsBlockedCore(taskId, cache);
        }

        private bool IsBlockedCore(Guid taskId, Dictionary<Guid, bool> cache)
        {
            if (cache.TryGetValue(taskId, out var cached))
                return cached;

            var task = GetTask(taskId);

            // completed tasks are never blocked
            if (task.Status == TaskStatus.Completed)
                return cache[taskId] = false;

            foreach (var depId in task.DependencyIds)
            {
                var dep = GetTask(depId);

                // If the direct dep isn't completed, you're blocked
                if (dep.Status != TaskStatus.Completed)
                    return cache[taskId] = true;

                // Otherwise if *that* dep is blocked (because of its deps), you're blocked
                if (IsBlockedCore(depId, cache))
                    return cache[taskId] = true;
            }

            return cache[taskId] = false;
        }

        public void AddDependency(Guid taskId, Guid dependencyId)
        {
            if (taskId == dependencyId)
                throw new InvalidOperationException("Task cannot depend on itself.");

            var task = GetTask(taskId);
            _ = GetTask(dependencyId); // ensure dependency exists

            if (WouldCreateCycle(taskId, dependencyId))
                throw new InvalidOperationException("Cannot add dependency because it would create a cycle.");

            task.AddDependency(dependencyId);
        }

        public bool WouldCreateCycle(Guid taskId, Guid dependencyId)
        {
            if (taskId == dependencyId) return true;

            // Adding edge: taskId -> dependencyId
            // This creates a cycle if dependencyId can already reach taskId.
            return CanReach(start: dependencyId, target: taskId);
        }

        private bool CanReach(Guid start, Guid target)
        {
            var visiting = new HashSet<Guid>();
            var visited = new HashSet<Guid>();

            bool Dfs(Guid current)
            {
                if (current == target) return true;

                if (visited.Contains(current)) return false;
                if (!visiting.Add(current)) return false;

                var task = GetTask(current);

                foreach (var dep in task.DependencyIds)
                {
                    if (Dfs(dep)) return true;
                }

                visiting.Remove(current);
                visited.Add(current);
                return false;
            }

            return Dfs(start);
        }

        public bool IsStale(Guid taskId)
        {
            var task = GetTask(taskId);
            return task.Status == TaskStatus.Completed && !IsSatisfied(taskId);
        }

        public bool IsSatisfied(Guid taskId)
        {
            var cache = new Dictionary<Guid, bool>();
            return IsSatisfiedCore(taskId, cache);
        }

        private bool IsSatisfiedCore(Guid taskId, Dictionary<Guid, bool> cache)
        {
            if (cache.TryGetValue(taskId, out var cached))
                return cached;

            var task = GetTask(taskId);

            foreach (var depId in task.DependencyIds)
            {
                var dep = GetTask(depId);

                // dependency itself must be completed
                if (dep.Status != TaskStatus.Completed)
                    return cache[taskId] = false;

                // and dependency's prerequisites must be satisfied too
                if (!IsSatisfiedCore(depId, cache))
                    return cache[taskId] = false;
            }

            return cache[taskId] = true;
        }

        public void AdvanceStatus(Guid taskId)
        {
            var task = _tasksById[taskId];
            switch (task.Status)
            {
                case TaskStatus.NotStarted or TaskStatus.Completed:
                    task.Start();
                    break;
                case TaskStatus.Started:
                    task.Complete();
                    break;
            }
        }
    }
}
