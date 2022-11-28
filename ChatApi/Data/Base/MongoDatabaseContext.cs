using MongoDB.Driver;

namespace ChatApi.Data.Base;

public class MongoDatabaseContext
{
    private readonly IMongoDatabase _database;
    private IMongoCollection<User>? _users;
    private IMongoCollection<ChatGroup>? _groups;
    private IMongoCollection<ChatMessage>? _messages;

    public MongoDatabaseContext(IMongoClient provider) => _database = provider.GetDatabase("Chat");

    public IMongoDatabase Database => _database;
    public IMongoCollection<User> Users => _users ??= _database.GetCollection<User>("users");
    public IMongoCollection<ChatGroup> Groups => _groups ??= _database.GetCollection<ChatGroup>("chats");
    public IMongoCollection<ChatMessage> Messages => _messages ??= _database.GetCollection<ChatMessage>("messages");
}