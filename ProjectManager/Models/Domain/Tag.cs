using System.Windows.Media;

namespace ProjectManager.Models.Domain;

public sealed class Tag
{
    public Tag(Guid id, string name, Color? color = null)
    {
        Id = id;
        Rename(name);
        Recolor(color);
    }

    public Guid Id { get; init; }
    public string Name { get; private set; }
    public Color? Color { get; private set; }

    public void Rename(string newName)
    {
        newName = (newName ?? "").Trim();
        if (newName.Length == 0)
            throw new ArgumentException("Tag name is required.", nameof(newName));

        Name = newName;
    }

    public void Recolor(Color? color)
    {
        Color = color;
    }
}