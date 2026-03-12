using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ProjectBoard.Converters;

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();

        if (string.IsNullOrWhiteSpace(hex))
            throw new JsonException("Color value was null or empty.");

        try
        {
            return (Color)ColorConverter.ConvertFromString(hex)!;
        }
        catch (Exception ex)
        {
            throw new JsonException($"Invalid color value '{hex}'.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}