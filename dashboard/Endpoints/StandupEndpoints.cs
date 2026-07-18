using dashboard.Data;

namespace dashboard.Endpoints;

public static class StandupEndpoints
{
    public static void MapStandupEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/standup");

        group.MapGet("/", async (MongoContext db) => await db.GetStandupNotesAsync());

        // Full overwrite of a day's note — used by the UI's textarea and for a clean rewrite.
        group.MapPut("/{date}", async (string date, SetStandupNoteRequest req, MongoContext db) =>
        {
            if (!IsValidDate(date)) return Results.BadRequest("Date must be yyyy-MM-dd.");
            var note = await db.SetStandupNoteAsync(date, req.Text ?? "");
            return Results.Ok(note);
        });

        // Adds a line to a day's note without clobbering what's already there — the
        // primary way to log work incrementally throughout the day.
        group.MapPost("/{date}/append", async (string date, AppendStandupNoteRequest req, MongoContext db) =>
        {
            if (!IsValidDate(date)) return Results.BadRequest("Date must be yyyy-MM-dd.");
            if (string.IsNullOrWhiteSpace(req.Text)) return Results.BadRequest("Text is required.");
            var note = await db.AppendStandupNoteAsync(date, req.Text);
            return Results.Ok(note);
        });

        group.MapDelete("/{date}", async (string date, MongoContext db) =>
        {
            var deleted = await db.DeleteStandupNoteAsync(date);
            return deleted ? Results.Ok() : Results.NotFound();
        });
    }

    private static bool IsValidDate(string date) => DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}

public record SetStandupNoteRequest(string? Text);
public record AppendStandupNoteRequest(string Text);
