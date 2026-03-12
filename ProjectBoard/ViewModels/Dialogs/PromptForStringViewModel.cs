using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectBoard.Stores;

namespace ProjectBoard.ViewModels.Dialogs;

public sealed class PromptForStringViewModel : ObservableObject
{
    private readonly Func<string, OperationResult> _tryAccept;
    private bool _closing;
    private string? _errorMessage;

    private string _value = "";

    public PromptForStringViewModel(
        string title,
        string label,
        string submitText,
        Func<string, OperationResult> tryAccept,
        string? initialValue = null)
    {
        Title = title;
        Label = label;
        SubmitText = submitText;
        _tryAccept = tryAccept;

        if (!string.IsNullOrEmpty(initialValue))
            Value = initialValue;

        SubmitCommand = new RelayCommand(() =>
        {
            if (_closing) return;

            var trimmed = (Value ?? "").Trim();
            if (trimmed.Length == 0)
            {
                ErrorMessage = "Please enter a value.";
                return;
            }

            var result = _tryAccept(trimmed);

            if (!result.Success)
            {
                ErrorMessage = result.Message ?? "Invalid value.";
                return;
            }

            Result = result;
            Value = trimmed;

            _closing = true;
            RequestCloseSuccess?.Invoke();
        });
    }

    public OperationResult? Result { get; private set; }

    public string Title { get; }
    public string Label { get; }
    public string SubmitText { get; }

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
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
    public bool CanSubmit => !string.IsNullOrEmpty(Value.Trim());

    public event Action? RequestCloseSuccess;
}