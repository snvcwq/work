using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dashboard.Models;

public class PullRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string TaskId { get; set; } = "";

    public string Number { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public PrStatus Status { get; set; } = PrStatus.WaitingForReview;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum PrStatus
{
    WaitingForReview,
    NeedsChanges,
    Completed,
    // Appended, not inserted — same int-serialization rule as WorkStatus/WorkItemType.
    // Displayed right after WaitingForReview in the dropdown regardless of this enum's
    // declaration order — see the Options param on StatusDropdown/StatusFilterDropdown.
    Approved
}
