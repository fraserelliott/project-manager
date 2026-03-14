using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
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

public sealed record OpenProjectIntent(string FilePath) : LaunchProjectIntent;

public sealed record CloneDemoProjectIntent(string Name, string FilePath) : LaunchProjectIntent;

public class StartupWindowViewModel : ObservableObject
{
    private readonly ObservableCollection<RecentProjectViewModel> _recentProjects = new();
    private readonly RecentProjectsService _recentProjectsService = new();
    private string _searchText = string.Empty;

    public StartupWindowViewModel()
    {
        _recentProjectsService.Load();
        NewProjectCommand = new RelayCommand(HandleNewProject);
        OpenProjectCommand = new RelayCommand(HandleOpenProject);
        CloneDemoProjectCommand = new RelayCommand(HandleCloneDemoProject);
        OpenRecentProjectCommand = new RelayCommand<RecentProjectViewModel>(HandleOpenRecentProject);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        RemoveRecentProjectCommand = new RelayCommand<RecentProjectViewModel>(RemoveRecentProject);

        RecentProjects = new ReadOnlyObservableCollection<RecentProjectViewModel>(_recentProjects);
        RecentProjectsView = CollectionViewSource.GetDefaultView(_recentProjects);
        RecentProjectsView.Filter = FilterRecentProjects;

        foreach (var recentProject in _recentProjectsService.RecentProjects)
            _recentProjects.Add(new RecentProjectViewModel(recentProject));
    }

    public ReadOnlyObservableCollection<RecentProjectViewModel> RecentProjects { get; }
    public ICollectionView RecentProjectsView { get; }
    public bool HasRecentProjects => _recentProjects.Count > 0;
    public RelayCommand NewProjectCommand { get; }
    public RelayCommand OpenProjectCommand { get; }
    public RelayCommand CloneDemoProjectCommand { get; }
    public RelayCommand<RecentProjectViewModel> OpenRecentProjectCommand { get; }
    public RelayCommand ClearSearchCommand { get; }
    public RelayCommand<RecentProjectViewModel> RemoveRecentProjectCommand { get; }
    public LaunchProjectIntent? LaunchIntent { get; private set; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                RecentProjectsView.Refresh();
        }
    }

    public event Action? RequestClose;

    private bool FilterRecentProjects(object obj)
    {
        if (obj is not RecentProjectViewModel recentProject) return false;

        if (string.IsNullOrEmpty(SearchText)) return true;

        return recentProject.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void RemoveRecentProject(RecentProjectViewModel recentProject)
    {
        var result = _recentProjectsService.RemoveRecentProject(recentProject.Id);
        if (result is UpdatedRecentProject updatedRecentProject)
        {
            _recentProjects.Remove(recentProject);
            RecentProjectsView.Refresh();
            OnPropertyChanged(nameof(HasRecentProjects));
        }
    }

    private void HandleOpenRecentProject(RecentProjectViewModel vm)
    {
        LaunchIntent = new OpenProjectIntent(vm.FilePath);
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

    private void HandleOpenProject()
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
        LaunchIntent = new OpenProjectIntent(filePath);
        RequestClose?.Invoke();
    }

    private void HandleCloneDemoProject()
    {
        var nameResult =
            new PromptService().PromptForString("New Project", "Project Name", "Create", ValidateProjectName);
        if (nameResult is not { Success: true }) return;
        if (nameResult.ResultAction is not StringResult stringResult) return;

        var name = stringResult.Value.Trim();

        SaveFileDialog saveFileDialog = new()
        {
            Title = "Clone Demo Project",
            Filter = "Project Board files (*.pbproj)|*.pbproj",
            DefaultExt = ".pbproj",
            FileName = $"{name}.pbproj"
        };

        var result = saveFileDialog.ShowDialog();
        if (result != true) return;

        var filePath = saveFileDialog.FileName;
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        LaunchIntent = new CloneDemoProjectIntent(name, filePath);

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