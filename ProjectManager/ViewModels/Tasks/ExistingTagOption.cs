namespace ProjectManager.ViewModels.Tasks;

public sealed class ExistingTagOption : AddTagOption
{
    public ExistingTagOption(TagViewModel tag)
    {
        Tag = tag;
    }

    public TagViewModel Tag { get; init; }

    public override string ToString()
    {
        return Tag.Name;
    }
}