using System.Windows.Media;
using ProjectBoard.Models.Persistence;

namespace ProjectBoard.Models.Domain;

public sealed class Project
{
    private readonly List<Note> _notes = new();
    private readonly Dictionary<Guid, Note> _notesById = new();
    private readonly List<Tag> _tags = new();
    private readonly Dictionary<Guid, Tag> _tagsById = new();
    private readonly List<TaskItem> _tasks = new();
    private readonly Dictionary<Guid, TaskItem> _tasksById = new();

    public Project(string name, Guid id)
    {
        Rename(name);
        Id = id;
    }

    public string Name { get; private set; } = "";
    public Guid Id { get; init; }
    public IReadOnlyList<Tag> Tags => _tags;
    public IReadOnlyList<TaskItem> Tasks => _tasks;
    public IReadOnlyList<Note> Notes => _notes;

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

        var task = new TaskItem(Guid.NewGuid(), name);

        _tasks.Add(task);
        _tasksById.Add(task.Id, task);
        return task;
    }

    public Note AddNote(string name, string text = "")
    {
        name = (name ?? "").Trim();
        if (name.Length == 0)
            throw new ArgumentException("Note name is required.", nameof(name));

        if (HasNoteWithName(name))
            throw new ArgumentException("Note name already exists.", nameof(name));

        var note = new Note(
            Guid.NewGuid(),
            name,
            text
        );

        _notes.Add(note);
        _notesById[note.Id] = note;
        return note;
    }

    public Tag AddTag(string name, Color? color)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0)
            throw new ArgumentException("Tag name is required.", nameof(name));

        if (HasTagWithName(name))
            throw new ArgumentException("Tag name already exists.", nameof(name));

        var tag = new Tag(
            Guid.NewGuid(),
            name,
            color
        );
        _tags.Add(tag);
        _tagsById[tag.Id] = tag;
        return tag;
    }

    public bool RenameTask(Guid taskId, string newName)
    {
        newName = (newName ?? "").Trim();
        if (newName.Length == 0)
            throw new ArgumentException("Task name is required.", nameof(newName));

        var task = GetTask(taskId) ?? throw new ArgumentException("Task not found.", nameof(taskId));

        // Allow "no-op rename"
        if (string.Equals(task.Name, newName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (_tasks.Any(t => t.Id != taskId && string.Equals(t.Name, newName, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Task name already exists.", nameof(newName));

        task.Rename(newName);
        return true;
    }

    public void RenameNote(Guid noteId, string newName)
    {
        newName = (newName ?? "").Trim();
        if (newName.Length == 0)
            throw new ArgumentException("Note name is required.", nameof(newName));

        var note = GetNote(noteId) ?? throw new ArgumentException("Note not found.", nameof(noteId));

        // Allow "no-op rename"
        if (string.Equals(note.Name, newName, StringComparison.OrdinalIgnoreCase))
            return;

        if (_notes.Any(n => n.Id != noteId && string.Equals(n.Name, newName, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Note name already exists.", nameof(newName));

        note.Rename(newName);
    }

    public void RenameTag(Guid tagId, string newName)
    {
        newName = (newName ?? "").Trim();
        if (newName.Length == 0)
            throw new ArgumentException("Tag name is required.", nameof(newName));

        var tag = GetTag(tagId) ?? throw new ArgumentException("Tag not found.", nameof(tagId));

        if (string.Equals(tag.Name, newName, StringComparison.OrdinalIgnoreCase))
            return;

        if (_tags.Any(t => t.Id != tagId && string.Equals(t.Name, newName, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Tag name already exists.", nameof(newName));

        tag.Rename(newName);
    }

    public bool RemoveTask(Guid taskId)
    {
        if (!_tasksById.Remove(taskId))
            return false;

        var idx = _tasks.FindIndex(t => t.Id == taskId);
        if (idx >= 0) _tasks.RemoveAt(idx);

        // Remove this task as a dependency from all remaining tasks
        foreach (var task in _tasks) task.RemoveDependency(taskId);

        return true;
    }

    public bool RemoveNote(Guid noteId)
    {
        if (!_notesById.Remove(noteId))
            return false;

        var idx = _notes.FindIndex(n => n.Id == noteId);
        if (idx >= 0) _notes.RemoveAt(idx);
        return true;
    }

    public bool RemoveTag(Guid tagId)
    {
        if (!_tagsById.Remove(tagId))
            return false;

        var idx = _tags.FindIndex(t => t.Id == tagId);
        if (idx >= 0) _tags.RemoveAt(idx);

        foreach (var task in Tasks) task.RemoveTag(tagId);

        return true;
    }

    public void RecolorTag(Guid tagId, Color? newColor)
    {
        var tag = GetTag(tagId) ?? throw new ArgumentException("Tag not found.", nameof(tagId));
        tag.Recolor(newColor);
    }

    public TaskItem? GetTask(Guid id)
    {
        return _tasksById.TryGetValue(id, out var task) ? task : null;
    }

    public Note? GetNote(Guid id)
    {
        return _notesById.TryGetValue(id, out var note) ? note : null;
    }

    public Tag? GetTag(Guid id)
    {
        return _tagsById.TryGetValue(id, out var tag) ? tag : null;
    }

    public bool HasTaskWithName(string name)
    {
        name = (name ?? "").Trim();
        return _tasks.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasTaskWithId(Guid id)
    {
        return _tasksById.Keys.Contains(id);
    }

    public bool HasNoteWithName(string name)
    {
        name = (name ?? "").Trim();
        return _notes.Any(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasTagWithName(string name)
    {
        name = (name ?? "").Trim();
        return _tags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasTagWithId(Guid id)
    {
        return _tagsById.Keys.Contains(id);
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
        return CanReach(dependencyId, taskId);
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
                if (Dfs(dep))
                    return true;

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

    public bool HasNoteWithId(Guid noteId)
    {
        return _notesById.ContainsKey(noteId);
    }

    public static Project FromData(ProjectData data)
    {
        var project = new Project(data.Name, data.Id);

        foreach (var tagData in data.Tags)
        {
            var tag = Tag.FromData(tagData);
            project._tags.Add(tag);
            project._tagsById[tag.Id] = tag;
        }

        foreach (var taskData in data.Tasks)
        {
            var task = TaskItem.FromData(taskData);
            project._tasks.Add(task);
            project._tasksById[task.Id] = task;
        }

        foreach (var noteData in data.Notes)
        {
            var note = Note.FromData(noteData);
            project._notes.Add(note);
            project._notesById[note.Id] = note;
        }

        return project;
    }

    public ProjectData ToData()
    {
        return new ProjectData
        {
            Id = Id,
            Name = Name,
            Tasks = _tasks.Select(t => t.ToData()).ToList(),
            Tags = _tags.Select(t => t.ToData()).ToList(),
            Notes = _notes.Select(n => n.ToData()).ToList()
        };
    }
}