using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dashboard.Models;

public class StandupNote
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    // "yyyy-MM-dd" — one note per calendar day, upserted by date rather than
    // addressed by Id, so "today's note" is always a single well-known document.
    public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

    public string Text { get; set; } = "";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
