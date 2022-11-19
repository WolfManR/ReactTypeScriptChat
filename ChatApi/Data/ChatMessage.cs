using MongoDB.Bson;

namespace ChatApi.Data;

class ChatMessage : Entity
{
    public string Message { get; set; } = null!;
    public ObjectId ChatGroupId { get; init; }
    public ObjectId UserId { get; init; }
}