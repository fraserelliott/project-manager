using System.IO;
using ProjectBoard.Models.Domain;

namespace ProjectBoard.Services;

public class FileProjectPersistence : IProjectPersistence
{
    private readonly string _filePath;
    private readonly IProjectSerializer _serializer;

    public FileProjectPersistence(string filePath, IProjectSerializer serializer)
    {
        _filePath = filePath;
        _serializer = serializer;
    }

    public void Save(Project project)
    {
        try
        {
            var json = _serializer.Serialize(project);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, true);
        }
        catch (Exception ex)
        {
            // TODO: logging
            throw new InvalidOperationException("Failed to save project.", ex);
        }
    }
}