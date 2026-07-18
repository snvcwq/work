using dashboard.Services;

namespace dashboard.Endpoints;

public static class NotifyEndpoints
{
    public static void MapNotifyEndpoints(this WebApplication app)
    {
        app.MapPost("/api/notify", (NotifyRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Message))
                return Results.BadRequest("Message is required.");

            DesktopNotifier.Send(string.IsNullOrWhiteSpace(req.Title) ? "dashboard" : req.Title, req.Message);
            return Results.Accepted();
        });
    }
}

public record NotifyRequest(string? Title, string Message);
