using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectManager.Controls;

public partial class SearchPicker : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(SearchPicker),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(SearchPicker),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(SearchPicker),
            new PropertyMetadata(-1));

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(
            nameof(SearchText),
            typeof(string),
            typeof(SearchPicker),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsPopupOpenProperty =
        DependencyProperty.Register(
            nameof(IsPopupOpen),
            typeof(bool),
            typeof(SearchPicker),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ItemChosenHandlerProperty =
        DependencyProperty.Register(
            nameof(ItemChosenHandler),
            typeof(Func<object?, bool>),
            typeof(SearchPicker),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(SearchPicker),
            new PropertyMetadata(null));

    public SearchPicker()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public bool IsPopupOpen
    {
        get => (bool)GetValue(IsPopupOpenProperty);
        set => SetValue(IsPopupOpenProperty, value);
    }

    public Func<object?, bool>? ItemChosenHandler
    {
        get => (Func<object?, bool>?)GetValue(ItemChosenHandlerProperty);
        set => SetValue(ItemChosenHandlerProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private void Reset()
    {
        SearchText = string.Empty;
        ResetSelection();
        IsPopupOpen = false;
    }

    private void ResetSelection()
    {
        SelectedItem = null;
        SelectedIndex = -1;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SearchPicker picker)
            return;

        if (picker.SelectedItem is null)
            return;

        if (picker.ItemsSource is { } items &&
            items.Cast<object?>().Contains(picker.SelectedItem))
            return;

        picker.ResetSelection();
    }

    private void ListView_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (SelectedItem == null)
            return;

        var result = ItemChosenHandler?.Invoke(SelectedItem);
        if (result == true)
            Reset();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (!IsPopupOpen)
            SearchBox.Focus();
        IsPopupOpen = !IsPopupOpen;
    }

    private void TextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var items = ItemsSource?.Cast<object>().ToList();
        if (items is null || items.Count == 0)
            return;

        switch (e.Key)
        {
            case Key.Down:
            {
                IsPopupOpen = true;

                if (SelectedIndex < 0)
                    SelectedIndex = 0;
                else if (SelectedIndex < items.Count - 1)
                    SelectedIndex++;

                e.Handled = true;
                break;
            }
            case Key.Up:
            {
                IsPopupOpen = true;

                if (SelectedIndex > 0)
                    SelectedIndex--;

                e.Handled = true;
                break;
            }
            case Key.Escape:
            {
                IsPopupOpen = false;
                break;
            }
            default:
            {
                IsPopupOpen = true;
                break;
            }
        }
    }
}