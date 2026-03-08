using ProjectManager.Stores;
using ProjectManager.ViewModels.Dialogs;
using ProjectManager.Views;
using System.Windows;
using System.Windows.Media;

namespace ProjectManager.Services;

public sealed class TagDialogService
{
    public OperationResult? PromptNewTag(Func<string, Color?, OperationResult> tryAccept, string startingName)
    {
        return OpenTagDialog(tryAccept, "New Tag", "Create", startingName, null);
    }

    public OperationResult? PromptTagUpdate(Func<string, Color?, OperationResult> tryAccept, string startingName, Color? startingColor)
    {
        return OpenTagDialog(tryAccept, "Edit Tag", "Save", startingName, startingColor);
    }

    private OperationResult? OpenTagDialog(Func<string, Color?, OperationResult> tryAccept, string title, string submitText, string? startingName, Color? startingColor)
    {
        var vm = new TagDialogViewModel(tryAccept, title, submitText, startingName, startingColor);
        var dlg = new TagDialog
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
