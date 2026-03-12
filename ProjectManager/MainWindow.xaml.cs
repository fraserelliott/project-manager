using System.Windows;
using System.Windows.Media;
using ProjectManager.Models.Domain;
using ProjectManager.Stores;
using ProjectManager.ViewModels;
using ProjectManager.Views;

namespace ProjectManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ShowDemoButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchProject(CreateDemoProject());
    }

    private void LaunchProject(Project project)
    {
        var projectSession = new ProjectSession(project);
        var projectWindow = new ProjectWindow
        {
            DataContext = new ProjectViewModel(projectSession)
        };

        Application.Current.MainWindow = projectWindow;
        projectWindow.Show();
        Close();
    }

    private Project CreateDemoProject()
    {
        var project = new Project("My Project");
        var taskA = project.AddTask("Create context for authentication");
        var b = project.AddTask("Create login page");
        var c = project.AddTask("Create endpoint /api/users/login");
        project.AddDependency(taskA.Id, b.Id);
        project.AddDependency(b.Id, c.Id);

        var tag = project.AddTag("test", (Color)ColorConverter.ConvertFromString("#EF4444"));
        taskA.AddTag(tag.Id);

        var tag2 = project.AddTag("a longer tag name", Colors.Green);
        taskA.AddTag(tag2.Id);

        project.AddTag("tag not on task", Colors.Pink);

        project.AddNote("my note", "# my header");

        return project;
    }
}