using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Markdig;
using Microsoft.Web.WebView2.Core;

namespace ProjectBoard.Controls;

public enum MarkdownViewMode
{
    Raw,
    Rendered
}

/// <summary>
///     Interaction logic for MarkdownView.xaml
/// </summary>
public partial class MarkdownView : UserControl
{
    private static readonly string MarkdownCss = TryLoadMarkdownCss();

    public static readonly DependencyProperty ViewModeProperty = DependencyProperty.Register
    (nameof(ViewMode), typeof(MarkdownViewMode), typeof(MarkdownView),
        new PropertyMetadata(MarkdownViewMode.Rendered, OnViewModeChanged));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register
        (nameof(Text), typeof(string), typeof(MarkdownView), new PropertyMetadata(string.Empty, OnTextChanged));

    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    private bool _allowOwnedNavigation;
    private bool _isWebViewReady;
    private bool _renderDirty = true;

    public MarkdownView()
    {
        InitializeComponent();
        Loaded += MarkdownView_Loaded;
    }

    public MarkdownViewMode ViewMode
    {
        get => (MarkdownViewMode)GetValue(ViewModeProperty);
        set => SetValue(ViewModeProperty, value);
    }

    public string Text
    {
        get => (string?)GetValue(TextProperty) ?? string.Empty;
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MarkdownView)d;
        control.HandleTextChanged();
    }

    private static void OnViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MarkdownView)d;
        control.HandleViewModeChanged();
    }


    private void HandleTextChanged()
    {
        _renderDirty = true;

        if (ViewMode == MarkdownViewMode.Rendered && _isWebViewReady)
        {
            RenderMarkdown(Text);
            _renderDirty = false;
        }
    }

    private void HandleViewModeChanged()
    {
        if (ViewMode == MarkdownViewMode.Rendered && _isWebViewReady && _renderDirty)
        {
            RenderMarkdown(Text);
            _renderDirty = false;
        }
    }

    private async void MarkdownView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isWebViewReady)
            return;

        await Browser.EnsureCoreWebView2Async();
        _isWebViewReady = true;

        Browser.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        Browser.CoreWebView2.NavigationStarting += Browser_NavigationStarting;

        if (ViewMode == MarkdownViewMode.Rendered && _renderDirty)
        {
            RenderMarkdown(Text);
            _renderDirty = false;
        }
    }

    private void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // Allow the navigation that this control itself just initiated.
        if (_allowOwnedNavigation)
        {
            _allowOwnedNavigation = false;
            return;
        }

        // Open external links in the user's browser.
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            e.Cancel = true;

            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri,
                UseShellExecute = true
            });

            return;
        }

        // Block everything else: back/forward, unexpected internal nav, etc.
        e.Cancel = true;
    }

    private void RenderMarkdown(string markdown)
    {
        var bodyHtml = Markdown.ToHtml(markdown ?? string.Empty, _pipeline);

        var fullHtml = $"""
                        <!DOCTYPE html>
                        <html>
                        <head>
                        <meta charset="utf-8" />
                        <style>
                        {MarkdownCss}
                        </style>
                        </head>
                        <body>
                        <article class="markdown-body">
                        {bodyHtml}
                        </article>
                        </body>
                        </html>
                        """;

        _allowOwnedNavigation = true;
        Browser.NavigateToString(fullHtml);
    }

    private static string TryLoadMarkdownCss()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Markdown", "github-markdown-dark.css");
        return File.Exists(path)
            ? File.ReadAllText(path)
            : """
              body { background: transparent; color: white; font-family: Segoe UI, sans-serif; }
              """;
    }
}