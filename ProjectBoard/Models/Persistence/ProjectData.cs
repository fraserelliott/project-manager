namespace ProjectBoard.Models.Persistence;

public sealed class ProjectData
{
    public string Name { get; set; } = "";
    public List<TaskItemData> Tasks { get; set; } = new();
    public List<TagData> Tags { get; set; } = new();
    public List<NoteData> Notes { get; set; } = new();
}