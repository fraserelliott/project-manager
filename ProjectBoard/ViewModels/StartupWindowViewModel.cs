using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ProjectBoard.Services;
using ProjectBoard.Stores;

namespace ProjectBoard.ViewModels;

public abstract record LaunchProjectIntent;

public sealed record NewProjectIntent(string Name, string FilePath) : LaunchProjectIntent;

public sealed record LoadProjectIntent(string FilePath) : LaunchProjectIntent;

public class StartupWindowViewModel : ObservableObject
{
    public StartupWindowViewModel()
    {
        NewProjectCommand = new RelayCommand(HandleNewProject);
        LoadProjectCommand = new RelayCommand(HandleLoadProject);
    }

    public RelayCommand NewProjectCommand { get; }
    public RelayCommand LoadProjectCommand { get; }
    public LaunchProjectIntent? LaunchIntent { get; private set; }
    public event Action? RequestClose;

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
}