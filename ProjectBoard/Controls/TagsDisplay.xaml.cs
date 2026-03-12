using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProjectBoard.ViewModels.Tasks;

namespace ProjectBoard.Controls;

/// <summary>
///     Interaction logic for TagsDisplay.xaml
/// </summary>
public partial class TagsDisplay : UserControl
{
    public static readonly DependencyProperty TagsProperty = DependencyProperty.Register
        (nameof(Tags), typeof(IEnumerable), typeof(TagsDisplay), new PropertyMetadata(null));

    public static readonly DependencyProperty ShowRemoveButtonProperty = DependencyProperty.Register
        (nameof(ShowRemoveButton), typeof(bool), typeof(TagsDisplay), new PropertyMetadata(false));

    public static readonly DependencyProperty RemoveTagCommandProperty = DependencyProperty.Register
        (nameof(RemoveTagCommand), typeof(ICommand), typeof(TagsDisplay), new PropertyMetadata(null));

    public static readonly DependencyProperty UpdateTagCommandProperty = DependencyProperty.Register
        (nameof(UpdateTagCommand), typeof(ICommand), typeof(TagsDisplay), new PropertyMetadata(null));

    public TagsDisplay()
    {
        InitializeComponent();
    }

    public IEnumerable Tags
    {
        get => (IEnumerable)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public bool ShowRemoveButton
    {
        get => (bool)GetValue(ShowRemoveButtonProperty);
        set => SetValue(ShowRemoveButtonProperty, value);
    }

    public ICommand? RemoveTagCommand
    {
        get => (ICommand?)GetValue(RemoveTagCommandProperty);
        set => SetValue(RemoveTagCommandProperty, value);
    }

    public ICommand? UpdateTagCommand
    {
        get => (ICommand?)GetValue(UpdateTagCommandProperty);
        set => SetValue(UpdateTagCommandProperty, value);
    }

    private void EditMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
            return;

        if (menuItem.DataContext is not TagViewModel tag)
            return;

        if (UpdateTagCommand?.CanExecute(tag.Id) == true) UpdateTagCommand.Execute(tag.Id);
    }
}