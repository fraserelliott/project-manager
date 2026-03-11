namespace ProjectManager.Models.Domain;

public sealed class Note
{
    public Note(Guid id, string name, string text)
    {
        Id = id;
        Rename(name);
        SetText(text);
    }

    public Guid Id { get; init; }
    public string Name { get; private set; }
    public string Text { get; private set; }

    public void Rename(string newName)
    {
        newName = newName ?? "";
        if (newName.Length == 0)
            throw new ArgumentException("Note name is required.", nameof(newName));

        Name = newName;
    }

    public void SetText(string text)
    {
        Text = text;
    }
}