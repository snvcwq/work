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
    public string? Assignee { get; set; }
    public WorkItemType Type { get; set; } = WorkItemType.Undefined;

    // Null = not (yet) claimed by an automated pipeline — every task today, and anything
    // you're handling manually. Non-null means an agent is driving it and this is exactly
    // where. Separate from Status on purpose: Status is the small, human-facing "what do I
    // need to know" signal; Stage is the orchestrator's own granular bookkeeping, which
    // would make Status noisy (15+ values, most never picked by a human) if merged in.
    public PipelineStage? Stage { get; set; }
    public string? StageDetail { get; set; }

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
    Done,
    // Appended, not inserted — enum values serialize to Mongo as their underlying
    // int, so a new member has to go last or it'll relabel every stored status.
    Removed,
    // Displayed between PrWaitingForReview and WaitingForRelease (see WorkStatusOrder in
    // Tasks.razor/TaskDetail.razor) despite landing at the end of the declaration here.
    PrApproved,
    PrMerged,
    Released,
    // Displayed right after New (see WorkStatusOrder in Tasks.razor/TaskDetail.razor)
    // despite landing at the end of the declaration here.
    Active
}

// Mirrors Azure Boards' work item types. Same append-only rule as WorkStatus above.
public enum WorkItemType
{
    Undefined,
    Task,
    Bug,
    UserStory,
    Feature,
    Epic
}

// An agent orchestrator's own step-by-step progress through a task, distinct from the
// human-facing WorkStatus above. Same append-only rule.
public enum PipelineStage
{
    Queued,
    SyncingToAzure,
    Coding,
    CodeReview,
    Testing,
    PushingBranch,
    AwaitingPrReview,
    FixingReviewComments,
    Merging,
    Closing,
    // The one stage you actually need to notice at a glance — surfaced loudly in the UI,
    // unlike the others which are quiet "for your curiosity" progress indicators.
    BlockedOnHuman
}
