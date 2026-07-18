using dashboard.Data;
using dashboard.Models;

namespace dashboard.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/events");

        group.MapGet("/", async (bool? unacknowledged, MongoContext db) =>
            await db.GetEventsAsync(unacknowledged ?? false));

        group.MapPost("/", async (CreateEventRequest req, MongoContext db) =>
        {
            if (!Enum.TryParse<EventType>(req.Type, ignoreCase: true, out var type))
                return Results.BadRequest($"Unknown event type '{req.Type}'. Valid: {string.Join(", ", Enum.GetNames<EventType>())}");

            var evt = await db.LogExternalEventAsync(new DashboardEvent
            {
                Type = type,
                Summary = req.Summary ?? "",
                TaskId = req.TaskId,
                TaskNumber = req.TaskNumber,
                PrId = req.PrId,
                PrNumber = req.PrNumber,
                Context = req.Context ?? new()
            });

            return Results.Created($"/api/events/{evt.Id}", evt);
        });

        group.MapPut("/{id}/ack", async (string id, MongoContext db) =>
        {
            var updated = await db.AcknowledgeEventAsync(id);
            return updated ? Results.Ok() : Results.NotFound();
        });

        group.MapPut("/ack-all", async (MongoContext db) =>
            Results.Ok(new { acknowledged = await db.AcknowledgeAllEventsAsync() }));
    }
}
