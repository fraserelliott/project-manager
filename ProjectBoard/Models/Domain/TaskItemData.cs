namespace ProjectBoard.Models.Domain;

public sealed class TaskItemData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Guid> TagIds { get; set; } = new();
    public List<Guid> DependencyIds { get; set; } = new();
    public TaskStatus Status { get; set; }
    public int Priority { get; set; }
}