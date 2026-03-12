using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjectBoard.Models.Domain;

namespace ProjectBoard.ViewModels.Tasks;

public sealed class TagViewModel : ObservableObject
{
    private readonly Tag _tag;

    public TagViewModel(Tag tag)
    {
        _tag = tag;
    }

    public string Name => _tag.Name;
    public Color? Color => _tag.Color;
    public Brush? Brush => _tag.Color.HasValue ? new SolidColorBrush(_tag.Color.Value) : null;
    public Guid Id => _tag.Id;

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(Brush));
    }
}