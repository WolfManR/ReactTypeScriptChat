using Redis.OM.Modeling;

namespace ChatApi.Data;

class Named : Entity
{
    [Searchable] public string Name { get; set; } = null!;
}