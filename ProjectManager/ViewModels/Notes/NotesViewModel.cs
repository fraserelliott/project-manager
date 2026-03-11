using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManager.Controls;
using ProjectManager.Stores;

namespace ProjectManager.ViewModels.Notes;

public sealed class NotesViewModel : ObservableObject
{
    private readonly ObservableCollection<NoteViewModel> _notes = new();
    private readonly ProjectSession _session;
    private bool _isEditing;
    private NoteViewModel? _selectedNote;

    public NotesViewModel(ProjectSession session)
    {
        _session = session;

        Notes = new ReadOnlyObservableCollection<NoteViewModel>(_notes);
        foreach (var note in _session.Project.Notes) _notes.Add(new NoteViewModel(this, note, _session));

        CloseNoteCommand = new RelayCommand(CloseNote);
    }

    public ReadOnlyObservableCollection<NoteViewModel> Notes { get; }
    public IRelayCommand CloseNoteCommand { get; init; }

    public NoteViewModel? SelectedNote
    {
        get => _selectedNote;
        set
        {
            if (SetProperty(ref _selectedNote, value))
            {
                OnPropertyChanged(nameof(HasSelectedNote));
                IsEditing = false;
            }
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value)) OnPropertyChanged(nameof(MarkdownViewMode));
        }
    }

    public MarkdownViewMode MarkdownViewMode => IsEditing ? MarkdownViewMode.Raw : MarkdownViewMode.Rendered;

    public bool HasSelectedNote => SelectedNote is not null;

    public string UnselectedText => _notes.Count == 0
        ? """
          # Notes

          You don't have any notes yet.

          Press **+** to create one.
          """
        : """
          # Notes

          Select a note from the list to view it.

          Did you know you can use **Markdown** in these notes?  
          This panel is also rendered using Markdown, so your notes will appear with the same formatting.

          Examples:
          - **bold**
          - *italic*
          - `inline code`
          """;

    private void CloseNote()
    {
        SelectedNote = null;
    }
}