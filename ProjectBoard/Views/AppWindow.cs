using System.Windows;
using System.Windows.Shell;

namespace ProjectBoard.Views;

public class AppWindow : Window
{
    public AppWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 36,
            ResizeBorderThickness = new Thickness(6),
            CornerRadius = new CornerRadius(0),
            GlassFrameThickness = new Thickness(0),
            UseAeroCaptionButtons = false
        });
    }

    public void MinimiseWindow()
    {
        WindowState = WindowState.Minimized;
    }

    public void ToggleMaximiseWindow()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    public void CloseWindow()
    {
        Close();
    }
}