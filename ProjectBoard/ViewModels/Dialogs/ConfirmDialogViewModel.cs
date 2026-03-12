using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProjectBoard.ViewModels.Dialogs;

public sealed class ConfirmDialogViewModel : ObservableObject
{
    public ConfirmDialogViewModel(string label, string submitText)
    {
        Label = label;
        SubmitText = submitText;
        SubmitCommand = new RelayCommand(Submit);
    }

    public string Label { get; }
    public string SubmitText { get; }
    public ICommand SubmitCommand { get; }
    public event Action? RequestCloseSuccess;

    private void Submit()
    {
        RequestCloseSuccess?.Invoke();
    }
}