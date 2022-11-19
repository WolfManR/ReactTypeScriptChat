using MongoDB.Bson;

namespace ChatApi.Data;

class Entity
{
    public ObjectId Id { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}