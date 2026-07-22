using System.Text.Json;
using System.Text.Json.Serialization;

namespace dashboard.Endpoints;

// Azure DevOps/GitHub numbers get passed around as either JSON strings or JSON numbers
// depending on the caller. These request fields are stored and compared as strings, so
// accept either wire shape rather than making callers remember to quote them.
public class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Expected string or number, got {reader.TokenType}.")
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
