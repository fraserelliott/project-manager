using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectBoard.Stores;

namespace ProjectBoard.ViewModels.Dialogs;

public sealed class TagDialogViewModel : ObservableObject
{
    private readonly Func<string, Color?, OperationResult> _tryAccept;
    private bool _closing;
    private string? _errorMessage;
    private string _name = "";

    private TagColorOption _selectedColorOption;

    public TagDialogViewModel(Func<string, Color?, OperationResult> tryAccept, string title, string submitText,
        string? startingName, Color? startingColor)
    {
        _tryAccept = tryAccept;
        Name = startingName ?? "";
        SelectedColorOption = AvailableColors.FirstOrDefault(c => c.Color == startingColor) ?? AvailableColors[0];
        Title = title;
        SubmitText = submitText;

        SubmitCommand = new RelayCommand(() =>
        {
            if (_closing) return;

            var trimmed = (Name ?? "").Trim();
            if (trimmed.Length == 0)
            {
                ErrorMessage = "Please enter a name.";
                return;
            }

            var result = _tryAccept(trimmed, SelectedColorOption.Color);

            if (!result.Success)
            {
                ErrorMessage = result.Message ?? "Invalid name.";
                return;
            }

            Result = result;
            Name = trimmed;

            _closing = true;
            RequestCloseSuccess?.Invoke();
        });
    }

    public OperationResult? Result { get; private set; }
    public string Title { get; init; }
    public string SubmitText { get; init; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            ErrorMessage = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSubmit));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand SubmitCommand { get; }
    public bool CanSubmit => !string.IsNullOrEmpty(Name.Trim());

    public TagColorOption SelectedColorOption
    {
        get => _selectedColorOption;
        set
        {
            if (_selectedColorOption != value)
            {
                _selectedColorOption = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<TagColorOption> AvailableColors { get; } = new()
    {
        new TagColorOption("None", null),
        new TagColorOption("Red", Hex("#EF4444")),
        new TagColorOption("Orange", Hex("#F97316")),
        new TagColorOption("Amber", Hex("#F59E0B")),
        new TagColorOption("Green", Hex("#22C55E")),
        new TagColorOption("Teal", Hex("#14B8A6")),
        new TagColorOption("Blue", Hex("#3B82F6")),
        new TagColorOption("Indigo", Hex("#6366F1")),
        new TagColorOption("Purple", Hex("#8B5CF6")),
        new TagColorOption("Pink", Hex("#EC4899"))
    };

    public event Action? RequestCloseSuccess;

    private static Color Hex(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex);
    }
}