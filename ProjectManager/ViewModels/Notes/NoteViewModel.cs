using CommunityToolkit.Mvvm.ComponentModel;
using ProjectManager.Models.Domain;
using ProjectManager.Stores;

namespace ProjectManager.ViewModels.Notes;

public sealed class NoteViewModel : ObservableObject
{
    private readonly Note _note;
    private readonly NotesViewModel _owner;
    private readonly ProjectSession _session;

    public NoteViewModel(NotesViewModel owner, Note note, ProjectSession session)
    {
        _owner = owner;
        _note = note;
        _session = session;
    }

    public Guid Id => _note.Id;
    public string Name => _note.Name;

    public string Text
    {
        get => _note.Text;
        set
        {
            var result = _session.SetTextOnNote(Id, value);
            if (result.Success) OnPropertyChanged();
        }
    }

    public override string ToString()
    {
        return Name;
    }
}