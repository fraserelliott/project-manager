using System.Windows;
using System.Windows.Media;
using ProjectManager.Models.Domain;
using ProjectManager.Stores;
using ProjectManager.ViewModels;

namespace ProjectManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // temporary sample data to prove bindings work
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

        var session = new ProjectSession(project);

        DataContext = new ShellViewModel(session);

        //var tagWindow = new TagDialog();
        //tagWindow.DataContext = new TagDialogViewModel(TagHandle, "Edit Tag", "Save", tag.Name, tag.Color);
        //tagWindow.Show();
    }

    private OperationResult TagHandle(string value)
    {
        return new OperationResult(true, new RefreshNone(), value);
    }
}