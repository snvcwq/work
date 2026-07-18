using dashboard.Data;
using dashboard.Models;

namespace dashboard.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks");

        group.MapGet("/", async (MongoContext db) => await db.GetAllTasksAsync());

        group.MapGet("/{id}", async (string id, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid task id.");

            var task = await db.FindTaskAsync(id);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        group.MapGet("/by-number/{number}", async (string number, MongoContext db) =>
        {
            var task = await db.FindTaskByNumberAsync(number);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        group.MapGet("/{id}/prs", async (string id, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid task id.");

            return Results.Ok(await db.GetPrsForTaskAsync(id));
        });

        group.MapPost("/", async (CreateTaskRequest req, MongoContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest("Title is required.");

            var status = WorkStatus.Undefined;
            if (!string.IsNullOrWhiteSpace(req.Status) && !Enum.TryParse(req.Status, ignoreCase: true, out status))
                return Results.BadRequest($"Unknown status '{req.Status}'. Valid: {string.Join(", ", Enum.GetNames<WorkStatus>())}");

            var type = WorkItemType.Undefined;
            if (!string.IsNullOrWhiteSpace(req.Type) && !Enum.TryParse(req.Type, ignoreCase: true, out type))
                return Results.BadRequest($"Unknown type '{req.Type}'. Valid: {string.Join(", ", Enum.GetNames<WorkItemType>())}");

            if (!string.IsNullOrWhiteSpace(req.Number))
            {
                var existing = await db.FindTaskByNumberAsync(req.Number);
                if (existing is not null)
                    return Results.Conflict(existing);
            }

            var task = await db.CreateTaskAsync(new WorkTask
            {
                Number = req.Number,
                Url = req.Url,
                Title = req.Title,
                Status = status,
                Type = type
            });

            return Results.Created($"/api/tasks/{task.Id}", task);
        });

        group.MapPut("/{id}/status", async (string id, UpdateStatusRequest req, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid task id.");

            if (!Enum.TryParse<WorkStatus>(req.Status, ignoreCase: true, out var status))
                return Results.BadRequest($"Unknown status '{req.Status}'. Valid: {string.Join(", ", Enum.GetNames<WorkStatus>())}");

            var updated = await db.SetTaskStatusAsync(id, status);
            return updated ? Results.Ok() : Results.NotFound();
        });

        group.MapPut("/{id}/type", async (string id, UpdateTypeRequest req, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid task id.");

            if (!Enum.TryParse<WorkItemType>(req.Type, ignoreCase: true, out var type))
                return Results.BadRequest($"Unknown type '{req.Type}'. Valid: {string.Join(", ", Enum.GetNames<WorkItemType>())}");

            var updated = await db.SetTaskTypeAsync(id, type);
            return updated ? Results.Ok() : Results.NotFound();
        });

        group.MapPut("/{id}/stage", async (string id, UpdateStageRequest req, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid task id.");

            PipelineStage? stage = null;
            if (!string.IsNullOrWhiteSpace(req.Stage))
            {
                if (!Enum.TryParse<PipelineStage>(req.Stage, ignoreCase: true, out var parsed))
                    return Results.BadRequest($"Unknown stage '{req.Stage}'. Valid: {string.Join(", ", Enum.GetNames<PipelineStage>())}");
                stage = parsed;
            }

            var updated = await db.SetTaskStageAsync(id, stage, req.Detail, req.Agent);
            return updated ? Results.Ok() : Results.NotFound();
        });

        group.MapPut("/{id}/assignee", async (string id, UpdateAssigneeRequest req, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid task id.");

            var updated = await db.SetTaskAssigneeAsync(id, req.Assignee);
            return updated ? Results.Ok() : Results.NotFound();
        });

        group.MapPost("/{id}/comments", async (string id, AddCommentRequest req, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid task id.");
            if (string.IsNullOrWhiteSpace(req.Text))
                return Results.BadRequest("Text is required.");

            var added = await db.AddTaskCommentAsync(id, req.Author ?? "unknown", req.Text);
            return added ? Results.Ok() : Results.NotFound();
        });

        group.MapPut("/reorder", async (ReorderRequest req, MongoContext db) =>
        {
            await db.ReorderTasksAsync(req.OrderedIds);
            return Results.Ok();
        });

        group.MapDelete("/{id}", async (string id, MongoContext db) =>
        {
            if (!MongoContext.IsValidObjectId(id))
                return Results.BadRequest("Invalid task id.");

            var deleted = await db.DeleteTaskCascadeAsync(id);
            return deleted ? Results.Ok() : Results.NotFound();
        });
    }
}
