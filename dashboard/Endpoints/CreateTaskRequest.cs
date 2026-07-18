namespace dashboard.Endpoints;

public record CreateTaskRequest(string Number, string Url, string Title, string? Status, string? Type);
