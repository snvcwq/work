using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dashboard.Models;

public class DashboardEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public EventType Type { get; set; }

    // Denormalized rather than a foreign key — agents polling the feed (and the events
    // tab itself) need to know what happened and where without a second lookup, and the
    // event should still read sensibly even after the underlying task/PR is deleted.
    public string? TaskId { get; set; }
    public string? TaskNumber { get; set; }
    public string? PrId { get; set; }
    public string? PrNumber { get; set; }

    public string Summary { get; set; } = "";
    public Dictionary<string, string> Context { get; set; } = new();

    // False for everything this app logs automatically on its own actions (task/PR create,
    // status/stage/assignee changes, comments) — those already show up on /live and on each
    // task's own detail page, so they don't need to double as an "events" notification too.
    // True only for events reported through POST /api/events — things noticed outside this
    // app's own scope (e.g. a Gmail-polling agent spotting a reassignment or PR approval).
    // The /events tab surfaces External events only; see GetNotableEventsAsync.
    public bool IsExternal { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Acknowledged { get; set; }
}

public enum EventType
{
    TaskCreated,
    TaskUpdated,
    TaskAssigned,
    TaskReassigned,
    TaskCommentAdded,
    PrCreated,
    PrUpdated,
    PrCommentAdded,
    PrApproved,
    // Appended, not inserted — same int-serialization rule as WorkStatus/WorkItemType.
    TaskStageChanged
}
