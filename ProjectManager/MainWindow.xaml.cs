using ProjectManager.Models.Domain;
using ProjectManager.ViewModels;
using System.Windows;

namespace ProjectManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // temporary sample data to prove bindings work
        var project = new Project("My Project");
        var a = project.AddTask("Create context for authentication");
        var b = project.AddTask("Create login page");
        var c = project.AddTask("Create endpoint /api/users/login");
        project.AddDependency(a.Id, b.Id);
        project.AddDependency(b.Id, c.Id);

        DataContext = new ProjectViewModel(project);
    }
}