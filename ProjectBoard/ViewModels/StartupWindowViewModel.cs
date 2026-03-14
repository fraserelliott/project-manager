using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ProjectBoard.Models;
using ProjectBoard.Models.Domain;
using ProjectBoard.Services;
using ProjectBoard.Stores;

namespace ProjectBoard.ViewModels;

public abstract record LaunchProjectIntent;

public sealed record NewProjectIntent(string Name, string FilePath) : LaunchProjectIntent;

public sealed record LoadProjectIntent(string FilePath) : LaunchProjectIntent;

public class StartupWindowViewModel : ObservableObject
{
    private readonly ObservableCollection<RecentProjectViewModel> _recentProjects = new();
    private readonly RecentProjectsService _recentProjectsService = new();

    public StartupWindowViewModel()
    {
        _recentProjectsService.Load();
        NewProjectCommand = new RelayCommand(HandleNewProject);
        LoadProjectCommand = new RelayCommand(HandleLoadProject);
        OpenRecentProjectCommand = new RelayCommand<RecentProjectViewModel>(HandleOpenRecentProject);
        RecentProjects = new ReadOnlyObservableCollection<RecentProjectViewModel>(_recentProjects);

        foreach (var recentProject in _recentProjectsService.RecentProjects)
            _recentProjects.Add(new RecentProjectViewModel(recentProject));
    }

    public ReadOnlyObservableCollection<RecentProjectViewModel> RecentProjects { get; }

    public RelayCommand NewProjectCommand { get; }
    public RelayCommand LoadProjectCommand { get; }
    public RelayCommand<RecentProjectViewModel> OpenRecentProjectCommand { get; }
    public LaunchProjectIntent? LaunchIntent { get; private set; }
    public event Action? RequestClose;

    private void HandleOpenRecentProject(RecentProjectViewModel vm)
    {
        LaunchIntent = new LoadProjectIntent(vm.FilePath);
        RequestClose?.Invoke();
    }

    private void HandleNewProject()
    {
        var nameResult =
            new PromptService().PromptForString("New Project", "Project Name", "Create", ValidateProjectName);
        if (nameResult is not { Success: true }) return;
        if (nameResult.ResultAction is not StringResult stringResult) return;

        var name = stringResult.Value.Trim();

        SaveFileDialog saveFileDialog = new()
        {
            Title = "Create Project",
            Filter = "Project Board files (*.pbproj)|*.pbproj",
            DefaultExt = ".pbproj",
            FileName = $"{name}.pbproj"
        };

        var result = saveFileDialog.ShowDialog();
        if (result != true) return;

        var filePath = saveFileDialog.FileName;
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        LaunchIntent = new NewProjectIntent(name, filePath);

        RequestClose?.Invoke();
    }

    private void HandleLoadProject()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Load Project",
            Filter = "Project Board files (*.pbproj)|*.pbproj",
            DefaultExt = ".pbproj"
        };

        var result = openFileDialog.ShowDialog();
        if (result != true) return;

        var filePath = openFileDialog.FileName;
        if (string.IsNullOrWhiteSpace(filePath)) return;
        LaunchIntent = new LoadProjectIntent(filePath);
        RequestClose?.Invoke();
    }

    private OperationResult ValidateProjectName(string name)
    {
        name = (name ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return new OperationResult(false, new RefreshNone(), "Project name must be provided.");

        return new OperationResult(true, new StringResult(name));
    }

    private void AddRecentProject(string name, string filePath, DateTime? lastOpened)
    {
        lastOpened = lastOpened ?? DateTime.Now;
    }

    public void UpdateRecentProject(Guid projectId, string projectName, string filePath, DateTime lastOpened)
    {
        var result = _recentProjectsService.UpdateRecentProject(projectId, projectName, filePath, lastOpened);
        switch (result)
        {
            case UpdatedRecentProject(Guid id):
            {
                var vm = _recentProjects.FirstOrDefault(p => p.Id == id);
                if (vm is null) return;
                vm.Refresh();
                break;
            }
            case AddedRecentProject(RecentProject project):
            {
                var vm = new RecentProjectViewModel(project);
                _recentProjects.Add(vm);
                break;
            }
        }
    }
}