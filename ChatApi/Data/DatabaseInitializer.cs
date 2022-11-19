using MongoDB.Driver;

namespace ChatApi.Data;

class DatabaseInitializer
{
	private readonly IMongoClient _mongoClient;

	public DatabaseInitializer(IMongoClient mongoClient)
	{
		_mongoClient = mongoClient;
	}

	public async ValueTask Initialize()
    {
	    var usersCollection = _mongoClient.GetDatabase("Chat").GetCollection<User>("users");
	    if (await usersCollection.CountDocumentsAsync(x => true) == 0)
	    {
		    await usersCollection.InsertOneAsync(new() { Group = "Admin", Name = "Admin" });
	    }
    }

    public async ValueTask Reinitialize()
    {
	    var database = _mongoClient.GetDatabase("Chat");

		var usersCollection = database.GetCollection<User>("users");
	    await usersCollection.DeleteManyAsync(x => true).ConfigureAwait(false);
	    if (await usersCollection.CountDocumentsAsync(x => true) == 0)
	    {
		    await usersCollection.InsertOneAsync(new() { Group = "Admin", Name = "Admin" });
	    }

	    var chatsCollection = database.GetCollection<ChatGroup>("chats");
	    await chatsCollection.DeleteManyAsync(x => true);

	    var messagesCollection = database.GetCollection<ChatMessage>("messages");
	    await messagesCollection.DeleteManyAsync(x => true);
    }
}