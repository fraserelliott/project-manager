using ProjectBoard.Models.Domain;

namespace ProjectBoard.Services;

public interface IProjectSerializer
{
    public string Serialize(Project project);
    Project Deserialize(string content);
}