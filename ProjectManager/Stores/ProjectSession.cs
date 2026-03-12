using System.Windows.Media;
using ProjectManager.Models.Domain;

namespace ProjectManager.Stores;

public abstract record RefreshScope;

public record RefreshNone : RefreshScope;

public record RefreshProject : RefreshScope;

public record RefreshTask(Guid TaskId) : RefreshScope;

public record RefreshTag(Guid TagId) : RefreshScope;

public record RefreshNote(Guid NoteId) : RefreshScope;

public record OperationResult(
    bool Success,
    RefreshScope Refresh,
    string? Message = null
);

public sealed class ProjectSession
{
    public ProjectSession(Project project)
    {
        Project = project;
    }

    public Project Project { get; }
    public bool IsDirty { get; private set; }

    public void Save()
    {
        IsDirty = false;
        // TODO: saving and persistence service
    }

    private void MarkDirty()
    {
        IsDirty = true;
        Save();
    }

    public bool IsTaskBlocked(Guid taskId)
    {
        return Project.IsBlocked(taskId);
    }

    public bool IsTaskStale(Guid taskId)
    {
        return Project.IsStale(taskId);
    }

    public OperationResult AdvanceStatus(Guid taskId)
    {
        if (!Project.HasTaskWithId(taskId))
            return new OperationResult(false, new RefreshProject(), "Task not found.");
        if (IsTaskBlocked(taskId))
            return new OperationResult(false, new RefreshNone(), "Task is blocked.");

        Project.AdvanceStatus(taskId);
        MarkDirty();
        return new OperationResult(true, new RefreshProject());
    }

    public OperationResult AddTask(string name)
    {
        if (Project.HasTaskWithName(name))
            return new OperationResult(false, new RefreshProject(), "A task with this name already exists.");

        var task = Project.AddTask(name);
        MarkDirty();
        return new OperationResult(true, new RefreshTask(task.Id));
    }

    public OperationResult AddNote(string name)
    {
        if (Project.HasNoteWithName(name))
            return new OperationResult(false, new RefreshNone(), "A note with this name already exists.");

        var note = Project.AddNote(name);
        MarkDirty();
        return new OperationResult(true, new RefreshNote(note.Id));
    }

    public OperationResult RemoveTask(Guid taskId)
    {
        if (!Project.HasTaskWithId(taskId))
            return new OperationResult(false, new RefreshProject(), "Task not found.");

        Project.RemoveTask(taskId);
        MarkDirty();
        return new OperationResult(true, new RefreshProject());
    }

    public OperationResult RemoveNote(Guid noteId)
    {
        if (!Project.HasNoteWithId(noteId))
            return new OperationResult(false, new RefreshNone(), "Note not found.");

        Project.RemoveNote(noteId);
        MarkDirty();
        return new OperationResult(true, new RefreshProject());
    }

    public OperationResult RenameTask(Guid taskId, string newName)
    {
        newName = (newName ?? "").Trim();
        var task = Project.GetTask(taskId);

        if (task == null)
            return new OperationResult(false, new RefreshProject(), "Task not found.");
        if (string.IsNullOrEmpty(newName))
            return new OperationResult(false, new RefreshProject(), "Task name must be provided.");
        if (string.Equals(task.Name, newName, StringComparison.OrdinalIgnoreCase))
            return new OperationResult(true, new RefreshNone());
        if (Project.HasTaskWithName(newName))
            return new OperationResult(false, new RefreshNone(), "A task with this name already exists.");

        Project.RenameTask(taskId, newName);
        MarkDirty();
        return new OperationResult(true, new RefreshTask(taskId));
    }

    public TaskItem? GetTask(Guid id)
    {
        return Project.GetTask(id);
    }

    public OperationResult AddTagToTask(Guid taskId, Guid tagId)
    {
        var task = Project.GetTask(taskId);
        var tag = Project.GetTag(tagId);

        if (task == null)
            return new OperationResult(false, new RefreshProject(), "Task not found.");
        if (!Project.HasTagWithId(tagId))
            return new OperationResult(false, new RefreshProject(), "Tag not found.");
        task.AddTag(tagId);
        MarkDirty();
        return new OperationResult(true, new RefreshTask(taskId));
    }

    public OperationResult AddTagToProject(string name, Color? color)
    {
        name = (name ?? "").Trim();

        if (string.IsNullOrEmpty(name))
            return new OperationResult(false, new RefreshNone(), "Tag name cannot be empty.");
        if (Project.HasTagWithName(name))
            return new OperationResult(false, new RefreshNone(), "A tag with this name already exists.");
        var tag = Project.AddTag(name, color);
        MarkDirty();
        return new OperationResult(true, new RefreshTag(tag.Id));
    }

    public OperationResult UpdateTag(Guid tagId, string newName, Color? newColor)
    {
        newName = (newName ?? "").Trim();

        if (string.IsNullOrEmpty(newName))
            return new OperationResult(false, new RefreshNone(), "Tag name cannot be empty.");

        var tag = Project.GetTag(tagId);
        if (tag is null)
            return new OperationResult(false, new RefreshProject(), "Tag not found.");

        if (string.Equals(tag.Name, newName, StringComparison.OrdinalIgnoreCase) && tag.Color.Equals(newColor))
            return new OperationResult(true, new RefreshNone());


        tag.Rename(newName);
        tag.Recolor(newColor);
        MarkDirty();
        return new OperationResult(true, new RefreshProject());
    }

    public Tag? GetTag(Guid id)
    {
        return Project.GetTag(id);
    }

    public bool WouldCreateCycle(Guid taskId, Guid dependencyId)
    {
        return Project.WouldCreateCycle(taskId, dependencyId);
    }

    public OperationResult RemoveTagFromTask(Guid taskId, Guid tagId)
    {
        var task = GetTask(taskId);
        if (task is null)
            return new OperationResult(false, new RefreshProject(), "Task not found.");

        task.RemoveTag(tagId);
        MarkDirty();
        return new OperationResult(true, new RefreshTask(taskId));
    }

    public OperationResult AddDependencyToTask(Guid taskId, Guid dependencyId)
    {
        var task = GetTask(taskId);
        if (task is null)
            return new OperationResult(false, new RefreshProject(), "Task not found.");

        if (Project.WouldCreateCycle(taskId, dependencyId))
            return new OperationResult(false, new RefreshNone(), "Adding this dependency would create a cycle.");

        task.AddDependency(dependencyId);
        MarkDirty();
        return new OperationResult(true, new RefreshProject());
    }

    public OperationResult RemoveDependencyFromTask(Guid taskId, Guid dependencyId)
    {
        var task = GetTask(taskId);
        if (task is null)
            return new OperationResult(false, new RefreshProject(), "Task not found.");

        var dependency = GetTask(dependencyId);
        if (task is null)
            return new OperationResult(false, new RefreshProject(), "Dependency not found.");

        task.RemoveDependency(dependencyId);
        MarkDirty();
        return new OperationResult(true, new RefreshProject());
    }

    public OperationResult UpdatePriorityOnTask(Guid taskId, int priority)
    {
        var task = GetTask(taskId);
        if (task is null)
            return new OperationResult(false, new RefreshProject(), "Task not found.");

        task.SetPriority(priority);
        MarkDirty();
        return new OperationResult(true, new RefreshTask(taskId));
    }

    public OperationResult UpdateDescriptionOnTask(Guid taskId, string description)
    {
        var task = GetTask(taskId);
        if (task is null)
            return new OperationResult(false, new RefreshProject(), "Task not found.");

        task.SetDescription(description);
        MarkDirty();
        return new OperationResult(true, new RefreshTask(taskId));
    }

    public bool HasTagWithName(string name)
    {
        return Project.HasTagWithName(name);
    }

    public OperationResult SetTextOnNote(Guid noteId, string newText)
    {
        var note = GetNote(noteId);
        if (note is null)
            return new OperationResult(false, new RefreshNone(), "Note not found.");

        if (newText == note.Text)
            return new OperationResult(true, new RefreshNone());

        note.SetText(newText);
        MarkDirty();
        return new OperationResult(true, new RefreshNote(noteId));
    }

    public Note? GetNote(Guid noteId)
    {
        return Project.GetNote(noteId);
    }
}