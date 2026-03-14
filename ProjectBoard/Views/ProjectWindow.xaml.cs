using System.ComponentModel;
using System.Windows;
using ProjectBoard.ViewModels;

namespace ProjectBoard.Views;

public partial class ProjectWindow : Window
{
    public ProjectWindow()
    {
        InitializeComponent();
        Closing += Window_Closing;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not ProjectViewModel vm) return;

        vm.Session.ExecuteSaveNow();
    }
}