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
    }

    public IMongoCollection<WorkTask> Tasks { get; }
    public IMongoCollection<PullRequest> Prs { get; }

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
        return task;
    }

    public async Task<bool> SetTaskStatusAsync(string id, WorkStatus status)
    {
        if (!IsValidObjectId(id)) return false;
        var update = Builders<WorkTask>.Update.Set(t => t.Status, status).Set(t => t.UpdatedAt, DateTime.UtcNow);
        var result = await Tasks.UpdateOneAsync(t => t.Id == id, update);
        return result.MatchedCount > 0;
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
        return pr;
    }

    public async Task<bool> SetPrStatusAsync(string id, PrStatus status)
    {
        if (!IsValidObjectId(id)) return false;
        var update = Builders<PullRequest>.Update.Set(p => p.Status, status).Set(p => p.UpdatedAt, DateTime.UtcNow);
        var result = await Prs.UpdateOneAsync(p => p.Id == id, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeletePrAsync(string id)
    {
        if (!IsValidObjectId(id)) return false;
        var result = await Prs.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }
}
