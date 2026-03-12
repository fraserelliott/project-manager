using System.IO;
using System.Windows;
using System.Windows.Media;
using ProjectBoard.Models.Domain;
using ProjectBoard.Services;
using ProjectBoard.Stores;
using ProjectBoard.ViewModels;
using ProjectBoard.Views;

namespace ProjectBoard;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ShowDemoButton_Click(object sender, RoutedEventArgs e)
    {
        var project = CreateDemoProject();
        var session = CreateSession(project, "test.json");
        LaunchProject(session);
    }

    private void LoadTestButton_Click(object sender, RoutedEventArgs e)
    {
        var session = LoadSession("test.json");
        LaunchProject(session);
    }

    private void LaunchProject(ProjectSession session)
    {
        var projectWindow = new ProjectWindow
        {
            DataContext = new ProjectViewModel(session)
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

        project.AddNote("my note", "# my heading");

        return project;
    }

    private ProjectSession CreateSession(Project project, string filePath)
    {
        return new ProjectSession(project, new FileProjectPersistence(filePath, new JsonProjectSerializer()));
    }

    private ProjectSession LoadSession(string filePath)
    {
        var serializer = new JsonProjectSerializer();
        var json = File.ReadAllText(filePath);
        Console.WriteLine(json);
        var project = serializer.Deserialize(json);

        return new ProjectSession(
            project,
            new FileProjectPersistence(filePath, serializer));
    }
}