using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectBoard.Views;

/// <summary>
///     Interaction logic for TaskDetailsWindow.xaml
/// </summary>
public partial class TaskDetailsWindow : Window
{
    public TaskDetailsWindow()
    {
        InitializeComponent();
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var textBox = (TextBox)sender;
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
            e.Handled = true;
        }
    }

    private void TagComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is ComboBox comboBox && !comboBox.IsDropDownOpen) comboBox.IsDropDownOpen = true;
    }
}