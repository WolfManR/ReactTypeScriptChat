using MongoDB.Bson;

namespace ChatApi.Data.Base;

public class Entity
{
    public ObjectId Id { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}