using Redis.OM.Modeling;

namespace ChatApi.Data;

class Entity
{
    [RedisIdField] public Ulid Id { get; set; }
}