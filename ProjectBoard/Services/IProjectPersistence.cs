using ProjectBoard.Models.Domain;

namespace ProjectBoard.Services;

public interface IProjectPersistence
{
    public void Save(Project project);
}