using System.Windows.Media;

namespace ProjectBoard.ViewModels.Dialogs;

public sealed class TagColorOption
{
    public TagColorOption(string name, Color? color)
    {
        Name = name;
        Color = color;
        SwatchBrush = color is null ? Brushes.Transparent : new SolidColorBrush(color.Value);
    }

    public string Name { get; }
    public Color? Color { get; }
    public Brush SwatchBrush { get; }

    public bool IsNone => Color is null;
}