using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProjectBoard.Views;

namespace ProjectBoard.Controls;

public partial class TitleBarControl : UserControl
{
    public static readonly DependencyProperty ShowMinimizeProperty =
        DependencyProperty.Register(
            nameof(ShowMinimize),
            typeof(bool),
            typeof(TitleBarControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowMaximizeProperty =
        DependencyProperty.Register(
            nameof(ShowMaximize),
            typeof(bool),
            typeof(TitleBarControl),
            new PropertyMetadata(true));

    public TitleBarControl()
    {
        InitializeComponent();
    }

    public bool ShowMinimize
    {
        get => (bool)GetValue(ShowMinimizeProperty);
        set => SetValue(ShowMinimizeProperty, value);
    }

    public bool ShowMaximize
    {
        get => (bool)GetValue(ShowMaximizeProperty);
        set => SetValue(ShowMaximizeProperty, value);
    }

    private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var window = Window.GetWindow(this);

        if (window == null)
            return;

        if (e.ClickCount == 2)
        {
            ToggleMaximise(window);
            return;
        }

        try
        {
            window.DragMove();
        }
        catch
        {
            // Ignore drag exceptions from rapid clicks/state changes
        }
    }

    private void Minimise_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window == null) return;

        if (window is AppWindow appWindow)
            appWindow.MinimiseWindow();
        else
            window.WindowState = WindowState.Minimized;
    }

    private void Maximise_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window == null) return;

        ToggleMaximise(window);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window == null) return;

        if (window is AppWindow appWindow)
            appWindow.CloseWindow();
        else
            window.Close();
    }

    private static void ToggleMaximise(Window window)
    {
        if (window is AppWindow appWindow)
        {
            appWindow.ToggleMaximiseWindow();
            return;
        }

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}