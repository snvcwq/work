namespace dashboard.Endpoints;

// Stage null/omitted clears it. Agent identifies which automation made the change, for
// the task's timeline — optional, since a human could clear/set this by hand too.
public record UpdateStageRequest(string? Stage, string? Detail, string? Agent);
