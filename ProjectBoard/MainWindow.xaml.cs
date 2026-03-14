using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProjectBoard.Models;
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
        var vm = new StartupWindowViewModel();
        vm.RequestClose += OnViewModelRequestClose;
        DataContext = vm;
    }

    private void OnViewModelRequestClose()
    {
        if (DataContext is not StartupWindowViewModel vm)
            return;
        try
        {
            switch (vm.LaunchIntent)
            {
                case NewProjectIntent newProject:
                {
                    var project = new Project(newProject.Name, Guid.NewGuid());
                    var serializer = new JsonProjectSerializer();
                    var projectSession = new ProjectSession(
                        project,
                        new FileProjectPersistenceService(newProject.FilePath, serializer));

                    projectSession.SaveNow();
                    RegisterRecentProject(vm, projectSession, newProject.FilePath);
                    LaunchProject(projectSession);
                    Close();
                    break;
                }

                case LoadProjectIntent loadProject:
                {
                    var projectSession = LoadSession(loadProject.FilePath);
                    if (projectSession is null) return;
                    RegisterRecentProject(vm, projectSession, loadProject.FilePath);
                    LaunchProject(projectSession);
                    Close();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to launch project:{Environment.NewLine}{ex.Message}",
                "Project Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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

    private void RegisterRecentProject(StartupWindowViewModel vm, ProjectSession session, string filePath)
    {
        vm.UpdateRecentProject(
            session.Project.Id,
            session.Project.Name,
            filePath,
            DateTime.Now);
    }

    private Project CreateDemoProject()
    {
        var project = new Project("My Project", Guid.NewGuid());
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
        return new ProjectSession(project, new FileProjectPersistenceService(filePath, new JsonProjectSerializer()));
    }

    private ProjectSession? LoadSession(string filePath)
    {
        var serializer = new JsonProjectSerializer();
        var json = File.ReadAllText(filePath);
        var project = serializer.Deserialize(json);

        return new ProjectSession(
            project,
            new FileProjectPersistenceService(filePath, serializer));
    }

    private void RecentProject_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
            return;

        if (sender is Border border && border.DataContext is RecentProjectViewModel recentProjectViewModel &&
            DataContext is StartupWindowViewModel vm)
            vm.OpenRecentProjectCommand.Execute(recentProjectViewModel);
    }

    private void OpenRecentProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is RecentProjectViewModel recentProjectViewModel &&
            DataContext is StartupWindowViewModel vm)
            vm.OpenRecentProjectCommand.Execute(recentProjectViewModel);
    }
}