namespace ProjectManager.ViewModels.Tasks;

public sealed class ExistingTagOption : AddTagOption
{
    public TagViewModel Tag { get; init; }

    public ExistingTagOption(TagViewModel tag)
    {
        Tag = tag;
    }

    public override string DisplayText => Tag.Name;
}
