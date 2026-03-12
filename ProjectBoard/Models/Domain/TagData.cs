using System.Windows.Media;

namespace ProjectBoard.Models.Domain;

public class TagData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Color? Color { get; set; }
}