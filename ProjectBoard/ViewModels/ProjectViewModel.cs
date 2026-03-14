using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectBoard.Stores;
using ProjectBoard.ViewModels.Notes;
using ProjectBoard.ViewModels.Tasks;

namespace ProjectBoard.ViewModels;

public sealed class ProjectViewModel : ObservableObject
{
    private ObservableObject _currentViewModel;

    public ProjectViewModel(ProjectSession session)
    {
        Session = session;
        Tasks = new TasksViewModel(session);
        Notes = new NotesViewModel(session);

        ShowTasksCommand = new RelayCommand(() => CurrentViewModel = Tasks);
        ShowNotesCommand = new RelayCommand(() => CurrentViewModel = Notes);

        CurrentViewModel = Tasks;
    }

    public string Title => $"Project Board - {Session.Project.Name}";
    public ProjectSession Session { get; }
    public TasksViewModel Tasks { get; }
    public NotesViewModel Notes { get; }

    public ObservableObject CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (SetProperty(ref _currentViewModel, value))
            {
                OnPropertyChanged(nameof(IsTasksActive));
                OnPropertyChanged(nameof(IsNotesActive));
            }
        }
    }

    public IRelayCommand ShowTasksCommand { get; }
    public IRelayCommand ShowNotesCommand { get; }

    public bool IsTasksActive => CurrentViewModel == Tasks;
    public bool IsNotesActive => CurrentViewModel == Notes;
}