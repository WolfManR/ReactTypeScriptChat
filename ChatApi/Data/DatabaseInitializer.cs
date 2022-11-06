using Redis.OM;

namespace ChatApi.Data;

class DatabaseInitializer
{
    private readonly RedisConnectionProvider _provider;

    private static readonly Type[] Documents =
    {
        typeof(User),
        typeof(ChatGroup),
        typeof(ChatMessage)
    };

    public DatabaseInitializer(RedisConnectionProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask Initialize()
    {
        foreach (var document in Documents)
            await _provider.Connection.CreateIndexAsync(document);
    }

    public async ValueTask Reinitialize()
    {
        var indexesDrop = Documents.Select(x => _provider.Connection.DropIndexAndAssociatedRecords(x)).ToList();
        if (indexesDrop.All(x => x)) await Initialize();
    }
}