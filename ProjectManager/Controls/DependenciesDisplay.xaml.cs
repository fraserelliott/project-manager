using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectManager.Controls;

/// <summary>
/// Interaction logic for DependencyDisplay.xaml
/// </summary>
public partial class DependenciesDisplay : UserControl
{
    public DependenciesDisplay()
    {
        InitializeComponent();
    }

    public IEnumerable Dependencies
    {
        get => (IEnumerable)GetValue(DependenciesProperty);
        set => SetValue(DependenciesProperty, value);
    }

    public static readonly DependencyProperty DependenciesProperty = DependencyProperty.Register
        (nameof(Dependencies), typeof(IEnumerable), typeof(DependenciesDisplay), new PropertyMetadata(null));

    public bool ShowRemoveButton
    {
        get => (bool)GetValue(ShowRemoveButtonProperty);
        set => SetValue(ShowRemoveButtonProperty, value);
    }

    public static readonly DependencyProperty ShowRemoveButtonProperty = DependencyProperty.Register
        (nameof(ShowRemoveButton), typeof(bool), typeof(DependenciesDisplay), new PropertyMetadata(false));

    public ICommand? RemoveDependencyCommand
    {
        get => (ICommand?)GetValue(RemoveDependencyCommandProperty);
        set => SetValue(RemoveDependencyCommandProperty, value);
    }

    public static readonly DependencyProperty RemoveDependencyCommandProperty = DependencyProperty.Register
        (nameof(RemoveDependencyCommand), typeof(ICommand), typeof(DependenciesDisplay), new PropertyMetadata(null));
}
