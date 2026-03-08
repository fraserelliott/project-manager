

using System.Windows.Media;

namespace ProjectManager.ViewModels.Dialogs;

public sealed class TagColorOption
{
    public string Name { get; }
    public Color? Color { get; }
    public Brush SwatchBrush { get; }

    public bool IsNone => Color is null;

    public TagColorOption(string name, Color? color)
    {
        Name = name;
        Color = color;
        SwatchBrush = color is null ? Brushes.Transparent : new SolidColorBrush(color.Value);
    }
}
