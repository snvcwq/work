using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dashboard.Models;

public class WorkTask
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Number { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public WorkStatus Status { get; set; } = WorkStatus.Undefined;

    // Higher priority sorts first (top of list). New tasks get max+1 so they land on top (LIFO).
    public long Priority { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum WorkStatus
{
    Undefined,
    New,
    Blocker,
    PrWaitingForReview,
    WaitingForRelease,
    Resolved,
    Done
}
