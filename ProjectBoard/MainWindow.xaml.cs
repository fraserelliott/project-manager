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
    public static readonly RoutedCommand CloseWindowCommand = new();

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

                case OpenProjectIntent loadProject:
                {
                    var projectSession = LoadSession(loadProject.FilePath);
                    if (projectSession is null) return;
                    RegisterRecentProject(vm, projectSession, loadProject.FilePath);
                    LaunchProject(projectSession);
                    Close();
                    break;
                }

                case CloneDemoProjectIntent cloneDemoProject:
                {
                    var project = CreateDemoProject(cloneDemoProject.Name);
                    Console.WriteLine($"Cloned project name {project.Name}, number of tasks = {project.Tasks.Count}");
                    var serializer = new JsonProjectSerializer();
                    var projectSession = new ProjectSession(
                        project,
                        new FileProjectPersistenceService(cloneDemoProject.FilePath, serializer));

                    projectSession.SaveNow();
                    RegisterRecentProject(vm, projectSession, cloneDemoProject.FilePath);
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

    private Project CreateDemoProject(string name)
    {
        var project = new Project(name, Guid.NewGuid());
        var taskA = project.AddTask("Create context for authentication");
        var taskB = project.AddTask("Create login page");
        var taskC = project.AddTask("Create endpoint /api/users/login");
        project.AddDependency(taskA.Id, taskB.Id);
        project.AddDependency(taskB.Id, taskC.Id);

        var tag = project.AddTag("backend", (Color)ColorConverter.ConvertFromString("#14B8A6"));
        taskC.AddTag(tag.Id);

        var tag2 = project.AddTag("frontend", (Color)ColorConverter.ConvertFromString("#6366F1"));
        taskA.AddTag(tag2.Id);
        taskB.AddTag(tag2.Id);

        var tag3 = project.AddTag("MVP", (Color)ColorConverter.ConvertFromString("#22C55E"));
        taskA.AddTag(tag3.Id);
        taskB.AddTag(tag3.Id);
        taskC.AddTag(tag3.Id);

        var text = """
                   # Project

                   ## Design ideas
                   - Make it work
                   - Make it look good
                   """;
        project.AddNote("my note", text);

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

    private void RemoveRecentProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is RecentProjectViewModel recentProjectViewModel &&
            DataContext is StartupWindowViewModel vm)
            vm.RemoveRecentProjectCommand.Execute(recentProjectViewModel);
    }

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StartupWindowViewModel vm) return;
        if (vm.NewProjectCommand.CanExecute(null))
            vm.NewProjectCommand.Execute(null);
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StartupWindowViewModel vm) return;
        if (vm.OpenProjectCommand.CanExecute(null))
            vm.OpenProjectCommand.Execute(null);
    }

    private void CloneDemoProject_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StartupWindowViewModel vm) return;
        if (vm.CloneDemoProjectCommand.CanExecute(null))
            vm.CloneDemoProjectCommand.Execute(null);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}