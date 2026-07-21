using dashboard.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace dashboard.Data;

public class MongoContext
{
    public MongoContext(IConfiguration config)
    {
        var connectionString = config["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var databaseName = config["Mongo:Database"] ?? "dashboard";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        Tasks = database.GetCollection<WorkTask>("tasks");
        Prs = database.GetCollection<PullRequest>("prs");
        Events = database.GetCollection<DashboardEvent>("events");
        StandupNotes = database.GetCollection<StandupNote>("standupNotes");
    }

    public IMongoCollection<WorkTask> Tasks { get; }
    public IMongoCollection<PullRequest> Prs { get; }
    public IMongoCollection<DashboardEvent> Events { get; }
    public IMongoCollection<StandupNote> StandupNotes { get; }

    private async Task LogEventAsync(EventType type, string summary, WorkTask? task = null, PullRequest? pr = null, Dictionary<string, string>? context = null)
    {
        await Events.InsertOneAsync(new DashboardEvent
        {
            Type = type,
            IsExternal = false,
            TaskId = task?.Id,
            TaskNumber = task?.Number,
            PrId = pr?.Id,
            PrNumber = pr?.Number,
            Summary = summary,
            Context = context ?? new()
        });
    }

    // Ids arrive from Claude Code tool calls and drag-drop payloads, either of which can carry a
    // stale or malformed id — validate before it reaches the driver, which throws FormatException
    // (not a query miss) on a bad ObjectId string.
    public static bool IsValidObjectId(string? id) => ObjectId.TryParse(id, out _);

    public Task<List<WorkTask>> GetAllTasksAsync() =>
        Tasks.Find(_ => true).SortByDescending(t => t.Priority).ToListAsync();

    public async Task<WorkTask?> FindTaskAsync(string id)
    {
        if (!IsValidObjectId(id)) return null;
        return await Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
    }

    public async Task<WorkTask?> FindTaskByNumberAsync(string number) =>
        await Tasks.Find(t => t.Number == number).FirstOrDefaultAsync();

    public Task<List<PullRequest>> GetPrsForTaskAsync(string taskId) =>
        Prs.Find(p => p.TaskId == taskId).SortBy(p => p.CreatedAt).ToListAsync();

    public Task<List<PullRequest>> GetAllPrsAsync() =>
        Prs.Find(_ => true).SortByDescending(p => p.CreatedAt).ToListAsync();

    // New tasks land on top of the board: priority = current max + 1.
    public async Task<WorkTask> CreateTaskAsync(WorkTask task)
    {
        var top = await Tasks.Find(_ => true).SortByDescending(t => t.Priority).Limit(1).FirstOrDefaultAsync();
        task.Priority = (top?.Priority ?? 0) + 1;
        await Tasks.InsertOneAsync(task);
        await LogEventAsync(EventType.TaskCreated, $"task #{task.Number} created: {task.Title}", task: task);
        return task;
    }

    public async Task<bool> SetTaskStatusAsync(string id, WorkStatus status)
    {
        if (!IsValidObjectId(id)) return false;
        var existing = await Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (existing is null) return false;

        var update = Builders<WorkTask>.Update.Set(t => t.Status, status).Set(t => t.UpdatedAt, DateTime.UtcNow);
        var result = await Tasks.UpdateOneAsync(t => t.Id == id, update);

        if (result.MatchedCount > 0 && existing.Status != status)
        {
            await LogEventAsync(EventType.TaskUpdated, $"task #{existing.Number} status: {existing.Status} -> {status}", task: existing,
                context: new() { ["from"] = existing.Status.ToString(), ["to"] = status.ToString() });
        }

        return result.MatchedCount > 0;
    }

    // stage: null clears it (hands the task back out of the automated pipeline).
    public async Task<bool> SetTaskStageAsync(string id, PipelineStage? stage, string? detail, string? agent)
    {
        if (!IsValidObjectId(id)) return false;
        var existing = await Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (existing is null) return false;

        var update = Builders<WorkTask>.Update
            .Set(t => t.Stage, stage)
            .Set(t => t.StageDetail, detail)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);
        var result = await Tasks.UpdateOneAsync(t => t.Id == id, update);

        if (result.MatchedCount > 0 && existing.Stage != stage)
        {
            var from = existing.Stage?.ToString() ?? "none";
            var to = stage?.ToString() ?? "none";
            var context = new Dictionary<string, string> { ["from"] = from, ["to"] = to };
            if (!string.IsNullOrWhiteSpace(agent)) context["agent"] = agent;
            if (!string.IsNullOrWhiteSpace(detail)) context["detail"] = detail;

            await LogEventAsync(EventType.TaskStageChanged, $"task #{existing.Number} stage: {from} -> {to}", task: existing, context: context);
        }

        return result.MatchedCount > 0;
    }

    // Includes events logged against the task's own PRs (approvals, comments, status
    // changes) — those are logged with only a PrId, not the parent TaskId, so this is a
    // small join rather than a plain TaskId filter, to give a complete "what happened to
    // this task" timeline rather than missing everything that happened on its PRs.
    public async Task<List<DashboardEvent>> GetEventsForTaskAsync(string taskId)
    {
        var prIds = await Prs.Find(p => p.TaskId == taskId).Project(p => p.Id).ToListAsync();
        return await Events.Find(e => e.TaskId == taskId || (e.PrId != null && prIds.Contains(e.PrId)))
            .SortByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> SetTaskTypeAsync(string id, WorkItemType type)
    {
        if (!IsValidObjectId(id)) return false;
        var update = Builders<WorkTask>.Update.Set(t => t.Type, type).Set(t => t.UpdatedAt, DateTime.UtcNow);
        var result = await Tasks.UpdateOneAsync(t => t.Id == id, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> SetTaskAssigneeAsync(string id, string assignee)
    {
        if (!IsValidObjectId(id)) return false;
        var existing = await Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (existing is null) return false;

        var update = Builders<WorkTask>.Update.Set(t => t.Assignee, assignee).Set(t => t.UpdatedAt, DateTime.UtcNow);
        var result = await Tasks.UpdateOneAsync(t => t.Id == id, update);

        if (result.MatchedCount > 0 && existing.Assignee != assignee)
        {
            var type = string.IsNullOrWhiteSpace(existing.Assignee) ? EventType.TaskAssigned : EventType.TaskReassigned;
            await LogEventAsync(type, $"task #{existing.Number} assigned to {assignee}", task: existing,
                context: new() { ["from"] = existing.Assignee ?? "", ["to"] = assignee });
        }

        return result.MatchedCount > 0;
    }

    public async Task<bool> AddTaskCommentAsync(string id, string author, string text)
    {
        if (!IsValidObjectId(id)) return false;
        var task = await Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (task is null) return false;

        await LogEventAsync(EventType.TaskCommentAdded, $"comment on task #{task.Number} by {author}", task: task,
            context: new() { ["author"] = author, ["text"] = text });
        return true;
    }

    // Deleting a task also deletes every PR linked to it — there is no orphan-PR state in this app.
    public async Task<bool> DeleteTaskCascadeAsync(string id)
    {
        if (!IsValidObjectId(id)) return false;
        await Prs.DeleteManyAsync(p => p.TaskId == id);
        var result = await Tasks.DeleteOneAsync(t => t.Id == id);
        return result.DeletedCount > 0;
    }

    // First id in the list gets the highest priority (top of the board).
    // Malformed ids are skipped rather than failing the whole reorder.
    public async Task ReorderTasksAsync(List<string> orderedIds)
    {
        var count = orderedIds.Count;
        for (var i = 0; i < count; i++)
        {
            if (!IsValidObjectId(orderedIds[i])) continue;
            var priority = count - i;
            await Tasks.UpdateOneAsync(t => t.Id == orderedIds[i], Builders<WorkTask>.Update.Set(t => t.Priority, (long)priority));
        }
    }

    public async Task<PullRequest?> FindPrAsync(string id)
    {
        if (!IsValidObjectId(id)) return null;
        return await Prs.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<PullRequest> CreatePrAsync(PullRequest pr)
    {
        await Prs.InsertOneAsync(pr);
        await LogEventAsync(EventType.PrCreated, $"pr #{pr.Number} opened: {pr.Title}", pr: pr);
        return pr;
    }

    public async Task<bool> SetPrStatusAsync(string id, PrStatus status)
    {
        if (!IsValidObjectId(id)) return false;
        var existing = await Prs.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (existing is null) return false;

        var update = Builders<PullRequest>.Update.Set(p => p.Status, status).Set(p => p.UpdatedAt, DateTime.UtcNow);
        var result = await Prs.UpdateOneAsync(p => p.Id == id, update);

        if (result.MatchedCount > 0 && existing.Status != status)
        {
            await LogEventAsync(EventType.PrUpdated, $"pr #{existing.Number} status: {existing.Status} -> {status}", pr: existing,
                context: new() { ["from"] = existing.Status.ToString(), ["to"] = status.ToString() });
        }

        return result.MatchedCount > 0;
    }

    public async Task<bool> AddPrCommentAsync(string id, string author, string text)
    {
        if (!IsValidObjectId(id)) return false;
        var pr = await Prs.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (pr is null) return false;

        await LogEventAsync(EventType.PrCommentAdded, $"comment on pr #{pr.Number} by {author}", pr: pr,
            context: new() { ["author"] = author, ["text"] = text });
        return true;
    }

    public async Task<bool> DeletePrAsync(string id)
    {
        if (!IsValidObjectId(id)) return false;
        var result = await Prs.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }

    public Task<List<DashboardEvent>> GetEventsAsync(bool unacknowledgedOnly = false) =>
        (unacknowledgedOnly ? Events.Find(e => !e.Acknowledged) : Events.Find(_ => true))
            .SortByDescending(e => e.CreatedAt)
            .ToListAsync();

    // What the events tab (and its unread badge) actually surfaces — External events only.
    // Every internal action this app logs for itself (task/PR create, status/stage/assignee
    // changes, comments) already has a home on /live and on the task's own detail page via
    // GetEventsForTaskAsync; it doesn't also need to show up as an "events" notification.
    // The events tab is specifically for things noticed outside this app's own scope (e.g. a
    // Gmail-polling agent reporting a reassignment or PR approval it saw in email).
    public Task<List<DashboardEvent>> GetNotableEventsAsync(bool unacknowledgedOnly = false)
    {
        var filter = Builders<DashboardEvent>.Filter.Eq(e => e.IsExternal, true);
        if (unacknowledgedOnly) filter &= Builders<DashboardEvent>.Filter.Eq(e => e.Acknowledged, false);
        return Events.Find(filter).SortByDescending(e => e.CreatedAt).ToListAsync();
    }

    // The one entry point for events this app can't detect itself — e.g. a scheduled
    // agent polling Gmail/Azure DevOps/GitHub reporting a reassignment or PR approval it
    // saw externally. Everything else is logged automatically by the methods above and
    // stays out of the /events tab (see GetNotableEventsAsync).
    public async Task<DashboardEvent> LogExternalEventAsync(DashboardEvent evt)
    {
        evt.IsExternal = true;
        evt.CreatedAt = DateTime.UtcNow;
        evt.Acknowledged = false;
        await Events.InsertOneAsync(evt);
        return evt;
    }

    public async Task<bool> AcknowledgeEventAsync(string id)
    {
        if (!IsValidObjectId(id)) return false;
        var result = await Events.UpdateOneAsync(e => e.Id == id, Builders<DashboardEvent>.Update.Set(e => e.Acknowledged, true));
        return result.MatchedCount > 0;
    }

    public async Task<long> AcknowledgeAllEventsAsync()
    {
        var result = await Events.UpdateManyAsync(e => !e.Acknowledged, Builders<DashboardEvent>.Update.Set(e => e.Acknowledged, true));
        return result.ModifiedCount;
    }

    public Task<List<StandupNote>> GetStandupNotesAsync() =>
        StandupNotes.Find(_ => true).SortByDescending(n => n.Date).ToListAsync();

    // Filter is a plain equality match on Date, so Mongo folds it into the inserted
    // document on upsert — no need for a separate SetOnInsert.
    public Task<StandupNote> SetStandupNoteAsync(string date, string text)
    {
        var update = Builders<StandupNote>.Update
            .Set(n => n.Text, text)
            .Set(n => n.UpdatedAt, DateTime.UtcNow);
        var options = new FindOneAndUpdateOptions<StandupNote> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
        return StandupNotes.FindOneAndUpdateAsync(n => n.Date == date, update, options);
    }

    public async Task<StandupNote> AppendStandupNoteAsync(string date, string text)
    {
        var existing = await StandupNotes.Find(n => n.Date == date).FirstOrDefaultAsync();
        var newText = string.IsNullOrWhiteSpace(existing?.Text) ? text : existing.Text.TrimEnd() + "\n" + text;
        return await SetStandupNoteAsync(date, newText);
    }

    public async Task<bool> DeleteStandupNoteAsync(string date)
    {
        var result = await StandupNotes.DeleteOneAsync(n => n.Date == date);
        return result.DeletedCount > 0;
    }
}
