namespace ProjectBoard.ViewModels.Tasks;

public sealed class AddDependencyOption
{
    public AddDependencyOption(TaskItemViewModel task)
    {
        Task = task;
    }

    public TaskItemViewModel Task { get; init; }

    public override string ToString()
    {
        return Task.Name;
    }
}