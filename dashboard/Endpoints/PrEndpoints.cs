using dashboard.Data;
using dashboard.Models;

namespace dashboard.Endpoints;

public static class PrEndpoints
{
    public static void MapPrEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/prs");

        group.MapGet("/", async (MongoContext db) => await db.GetAllPrsAsync());

        group.MapGet("/{id}", async (string id, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid pr id.");

            var pr = await db.FindPrAsync(id);
            return pr is null ? Results.NotFound() : Results.Ok(pr);
        });

        group.MapPost("/", async (CreatePrRequest req, MongoContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest("Title is required.");

            var taskId = req.TaskId;
            if (string.IsNullOrWhiteSpace(taskId) && !string.IsNullOrWhiteSpace(req.TaskNumber))
            {
                var task = await db.FindTaskByNumberAsync(req.TaskNumber);
                if (task is null)
                    return Results.BadRequest($"No task found with number '{req.TaskNumber}'.");
                taskId = task.Id;
            }

            if (string.IsNullOrWhiteSpace(taskId) || !MongoContext.IsValidObjectId(taskId))
                return Results.BadRequest("A valid taskId or taskNumber is required.");

            var status = PrStatus.WaitingForReview;
            if (!string.IsNullOrWhiteSpace(req.Status) && !Enum.TryParse(req.Status, ignoreCase: true, out status))
                return Results.BadRequest($"Unknown status '{req.Status}'. Valid: {string.Join(", ", Enum.GetNames<PrStatus>())}");

            var pr = await db.CreatePrAsync(new PullRequest
            {
                TaskId = taskId,
                Number = req.Number,
                Url = req.Url,
                Title = req.Title,
                Status = status
            });

            return Results.Created($"/api/prs/{pr.Id}", pr);
        });

        group.MapPut("/{id}/status", async (string id, UpdateStatusRequest req, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid pr id.");

            if (!Enum.TryParse<PrStatus>(req.Status, ignoreCase: true, out var status))
                return Results.BadRequest($"Unknown status '{req.Status}'. Valid: {string.Join(", ", Enum.GetNames<PrStatus>())}");

            var updated = await db.SetPrStatusAsync(id, status);
            return updated ? Results.Ok() : Results.NotFound();
        });

        group.MapDelete("/{id}", async (string id, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid pr id.");

            var deleted = await db.DeletePrAsync(id);
            return deleted ? Results.Ok() : Results.NotFound();
        });
    }
}
