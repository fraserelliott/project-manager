namespace ProjectManager.Models.Domain
{
    public sealed class Note
    {
        public Guid Id { get; init; }
        public string Name { get; private set; }
        public string Markdown { get; private set; }

        public Note(Guid id, string name, string markdown)
        {
            Id = id;
            Rename(name);
            SetMarkdown(markdown);
        }

        public void Rename(string newName)
        {
            newName = newName ?? "";
            if (newName.Length == 0)
                throw new ArgumentException("Note name is required.", nameof(newName));

            Name = newName;
        }

        public void SetMarkdown(string markdown)
        {
            Markdown = markdown?.Trim() ?? "";
        }
    }
}
