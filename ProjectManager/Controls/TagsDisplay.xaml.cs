using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectManager.Controls;

/// <summary>
/// Interaction logic for TagsDisplay.xaml
/// </summary>
public partial class TagsDisplay : UserControl
{
    public TagsDisplay()
    {
        InitializeComponent();
    }

    public IEnumerable Tags
    {
        get => (IEnumerable)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public static readonly DependencyProperty TagsProperty = DependencyProperty.Register
        (nameof(Tags), typeof(IEnumerable), typeof(TagsDisplay), new PropertyMetadata(null));

    public bool ShowRemoveButton
    {
        get => (bool)GetValue(ShowRemoveButtonProperty);
        set => SetValue(ShowRemoveButtonProperty, value);
    }

    public static readonly DependencyProperty ShowRemoveButtonProperty = DependencyProperty.Register
        (nameof(ShowRemoveButton), typeof(bool), typeof(TagsDisplay), new PropertyMetadata(false));

    public ICommand? RemoveTagCommand
    {
        get => (ICommand?)GetValue(RemoveTagCommandProperty);
        set => SetValue(RemoveTagCommandProperty, value);
    }

    public static readonly DependencyProperty RemoveTagCommandProperty = DependencyProperty.Register
        (nameof(RemoveTagCommand), typeof(ICommand), typeof(TagsDisplay), new PropertyMetadata(null));
}
