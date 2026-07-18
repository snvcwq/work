namespace dashboard.Endpoints;

// For events this app can't detect itself — a scheduled agent reporting something it
// saw on Azure DevOps/GitHub (a reassignment, a PR approval) that has no corresponding
// action inside this app to hang the event off of.
public record CreateEventRequest(
    string Type,
    string? Summary,
    string? TaskId,
    string? TaskNumber,
    string? PrId,
    string? PrNumber,
    Dictionary<string, string>? Context);
