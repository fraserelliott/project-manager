using ProjectBoard.Models.Domain;

namespace ProjectBoard.Services;

public interface IProjectPersistenceService
{
    public void Save(Project project);
}