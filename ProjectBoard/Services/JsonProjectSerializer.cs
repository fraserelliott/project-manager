using System.Text.Json;
using ProjectBoard.Converters;
using ProjectBoard.Models.Domain;

namespace ProjectBoard.Services;

public class JsonProjectSerializer : IProjectSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new ColorJsonConverter()
        }
    };

    public string Serialize(Project project)
    {
        var data = project.ToData();
        return JsonSerializer.Serialize(data, _options);
    }

    public Project Deserialize(string content)
    {
        var data = JsonSerializer.Deserialize<ProjectData>(content, _options) ?? throw new InvalidOperationException(
            "Failed to deserialize project.");
        return Project.FromData(data);
    }
}