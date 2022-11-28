using ChatApi.Data.Base;
using MongoDB.Bson;

namespace ChatApi.Data;

public class ChatGroup : Named
{
    public ChatGroup() { }
    public ChatGroup(string name) => Name = name;

    public List<ObjectId> Users { get; init; } = new();

    public ChatGroup AddUser(User user)
    {
        if (!Users.Contains(user.Id))
        {
            Users.Add(user.Id);
        }

        return this;
    }
}