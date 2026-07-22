using System.Text.Json.Serialization;

namespace dashboard.Endpoints;

public record CreateTaskRequest(
    [property: JsonConverter(typeof(FlexibleStringConverter))] string Number,
    string Url,
    string Title,
    string? Status,
    string? Type);
