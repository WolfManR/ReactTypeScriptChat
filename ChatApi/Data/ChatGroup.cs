using Redis.OM.Modeling;

namespace ChatApi.Data;

[Document(StorageType = StorageType.Json)]
class ChatGroup : Named
{
    public ChatGroup() { }
    public ChatGroup(string name) => Name = name;

    [Indexed] public List<Ulid> Users { get; init; } = new();

    public ChatGroup AddUser(User user)
    {
        if (!Users.Contains(user.Id))
        {
            Users.Add(user.Id);
        }

        return this;
    }
}