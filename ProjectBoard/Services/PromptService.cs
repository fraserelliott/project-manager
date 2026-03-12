using System.Windows;
using ProjectBoard.Stores;
using ProjectBoard.ViewModels.Dialogs;
using ProjectBoard.Views;

namespace ProjectBoard.Services;

public sealed class PromptService
{
    public OperationResult? PromptForString(
        string title,
        string label,
        string submitText,
        Func<string, OperationResult> tryAccept,
        string? initialValue = null)
    {
        var vm = new PromptForStringViewModel(title, label, submitText, tryAccept, initialValue);

        var dlg = new PromptForStringDialog
        {
            DataContext = vm
        };

        vm.RequestCloseSuccess += () =>
        {
            dlg.DialogResult = true;
            dlg.Close();
        };

        dlg.Owner = Application.Current?.MainWindow;

        var ok = dlg.ShowDialog() == true;
        if (!ok) return null;

        return vm.Result;
    }
}