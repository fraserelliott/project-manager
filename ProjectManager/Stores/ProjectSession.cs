using ProjectManager.Models.Domain;
using System.Windows.Media;

namespace ProjectManager.Stores;

public abstract record RefreshScope;
public record RefreshNone : RefreshScope;
public record RefreshProject : RefreshScope;
public record RefreshTask(Guid TaskId) : RefreshScope;
public record RefreshTag(Guid TagId) : RefreshScope;

public record OperationResult(
    bool Success,
    RefreshScope Refresh,
    string? Message = null
    );

public sealed class ProjectSession
{
    public Project Project { get; }
    public bool IsDirty { get; private set; }

    public ProjectSession(Project project)
    {
        Project = project;
    }

    public void Save()
    {
        IsDirty = false;
        // TODO: saving and persistence service
    }

    private void MarkDirty()
    {
        IsDirty = true;
        Save(); // later: debounce/autosave
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

    public OperationResult RemoveTask(Guid taskId)
    {
        if (!Project.HasTaskWithId(taskId))
            return new OperationResult(false, new RefreshProject(), "Task not found.");

        Project.RemoveTask(taskId);
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

    public TaskItem? GetTask(Guid id) => Project.GetTask(id);

    public OperationResult AddTag(Guid taskId, string name, Color? color)
    {
        name = (name ?? "").Trim();
        var task = Project.GetTask(taskId);

        if (task == null)
            return new OperationResult(false, new RefreshProject(), "Task not found.");
        if (Project.HasTagWithName(name))
            return new OperationResult(false, new RefreshNone(), "A tag with this name already exists.");
        var tag = Project.AddTag(name, color);
        task.AddTag(tag.Id);

        MarkDirty();
        return new OperationResult(true, new RefreshTag(tag.Id));
    }

    public Tag? GetTag(Guid id) => Project.GetTag(id);

    public bool WouldCreateCycle(Guid taskId, Guid dependencyId)
    {
        return Project.WouldCreateCycle(taskId, dependencyId);
    }
}
