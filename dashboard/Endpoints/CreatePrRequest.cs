using System.Text.Json.Serialization;

namespace dashboard.Endpoints;

public record CreatePrRequest(
    string? TaskId,
    [property: JsonConverter(typeof(FlexibleStringConverter))] string? TaskNumber,
    [property: JsonConverter(typeof(FlexibleStringConverter))] string Number,
    string Url,
    string Title,
    string? Status);
