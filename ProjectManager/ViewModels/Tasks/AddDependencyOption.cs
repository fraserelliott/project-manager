namespace ProjectManager.ViewModels.Tasks;

public sealed class AddDependencyOption
{
    public TaskItemViewModel Task { get; init; }

    public AddDependencyOption(TaskItemViewModel task)
    {
        Task = task;
    }

    public string DisplayText => Task.Name;
}
