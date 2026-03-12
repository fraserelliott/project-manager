namespace ProjectBoard.ViewModels.Tasks;

public sealed class CreateTagOption : AddTagOption
{
    public CreateTagOption(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override string ToString()
    {
        return $"Create tag \"{Name}\"";
    }
}