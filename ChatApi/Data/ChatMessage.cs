using Redis.OM.Modeling;

namespace ChatApi.Data;

[Document(StorageType = StorageType.Json)]
class ChatMessage : Entity
{
    public string Message { get; set; } = null!;
    [Indexed] public Ulid ChatGroupId { get; init; }
    [Indexed] public Ulid UserId { get; init; }
}