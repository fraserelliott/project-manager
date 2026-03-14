using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProjectBoard.ViewModels;

namespace ProjectBoard.Views;

public partial class ProjectWindow : Window
{
    public static readonly RoutedCommand CloseProjectCommand = new();
    public static readonly RoutedCommand CloseWindowCommand = new();

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

    private void CloseProject_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = new MainWindow();
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }

    private void NewTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;

        if (menuItem.DataContext is not ProjectViewModel vm) return;

        if (vm.Tasks.NewTaskCommand.CanExecute(null))
            vm.Tasks.NewTaskCommand.Execute(null);
    }

    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;
        if (menuItem.DataContext is not ProjectViewModel vm) return;

        if (vm.Notes.NewNoteCommand.CanExecute(null))
            vm.Notes.NewNoteCommand.Execute(null);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}