using System.Collections;

using ChatApi.Contracts;
using ChatApi.Results;

using MongoDB.Bson;
using MongoDB.Driver;

namespace ChatApi.Data;

class ChatStorage
{
	private readonly IMongoClient _provider;
	private readonly IMongoDatabase _database;
	private IMongoCollection<User>? _users;
	private IMongoCollection<ChatGroup>? _groups;
	private IMongoCollection<ChatMessage>? _messages;

	public ChatStorage(IMongoClient provider)
	{
		_provider = provider;
		_database = provider.GetDatabase("Chat");
	}

	private IMongoCollection<User> Users => _users ??= _database.GetCollection<User>("users");
	private IMongoCollection<ChatGroup> Groups => _groups ??= _database.GetCollection<ChatGroup>("chats");
	private IMongoCollection<ChatMessage> Messages => _messages ??= _database.GetCollection<ChatMessage>("messages");

	public async Task<User> SignIn(string name)
	{
		var user = await Users.Find(x => x.Name == name).FirstOrDefaultAsync();
		if (user is not null)
		{
			return user;
		}

		user = new()
		{
			Name = name,
			Group = "user"
		};
		await Users.InsertOneAsync(user);
		return user;
	}

	public async Task<IEnumerable> GetUsers()
	{
		var cursor = await Users.FindAsync(x => true);
		List<object> usersInfo = new();

		var chatInfoProjection = Builders<ChatGroup>.Projection.Expression<ChatInfo>(x => new(x.Name, x.Id.ToString(), GetChatLastMessage(x.Id)));

		foreach (var user in cursor.ToEnumerable())
		{
			var groups = Groups.Find(x => x.Users.Contains(user.Id))
				.Project(chatInfoProjection)
				.ToList();
			usersInfo.Add(new { name = user.Name, id = user.Id.ToString(), chats = groups.ToArray() });
		}

		return usersInfo.ToArray();
	}

	private string GetChatLastMessage(ObjectId id)
	{
		return Messages.Find(t => t.ChatGroupId == id).SortByDescending(x => x.Created).FirstOrDefault()?.Message ?? string.Empty;
	}

	public IEnumerable<ChatInfo> GetChats()
	{
		var chatInfoProjection = Builders<ChatGroup>.Projection.Expression<ChatInfo>(x => new(x.Name, x.Id.ToString(), GetChatLastMessage(x.Id)));

		var groups = Groups.Find(x => true)
				.Project(chatInfoProjection)
				.ToList();
		return groups;
	}

	public async Task<IEnumerable> GetMessages()
	{
		var messages = new List<object>();
		var messagesCursor = await Messages.Find(x => true).ToCursorAsync();

		foreach (var x in messagesCursor.ToEnumerable())
		{
			var userName = Users.Find(c => c.Id == x.UserId).FirstOrDefault()?.Name ?? string.Empty;
			var chatGroup = Groups.Find(c => c.Id == x.ChatGroupId).FirstOrDefault()?.Name ?? string.Empty;
			messages.Add(new { userName, x.Message, chatGroup });
		}

		return messages;
	}

	public Result<string> CreateChatGroup(string name, string userId)
	{
		var actualUserId = ObjectId.Parse(userId);
		var user = Users.Find(x => x.Id == actualUserId).FirstOrDefault();
		if (user is null) return Result<string>.Failure(Errors.Accounts.UserNotFound);

		var group = new ChatGroup(name) { Created = DateTime.UtcNow }.AddUser(user);
		Groups.InsertOne(group);

		return Result<string>.Success(group.Id.ToString());
	}

	public Result JoinGroup(string userId, string chatGroupId)
	{
		var actualUserId = ObjectId.Parse(userId);
		var actualChatId = ObjectId.Parse(chatGroupId);

		var user = Users.Find(x => x.Id == actualUserId).FirstOrDefault();
		if (user is null) return Result.Failure(Errors.Accounts.UserNotFound);

		Groups.FindOneAndUpdate(x => x.Id == actualChatId, Builders<ChatGroup>.Update.Push(x => x.Users, user.Id));

		return Result.Success();
	}

	public Result<string> AddMessage(string userId, string message, string chatGroupId)
	{
		var actualUserId = ObjectId.Parse(userId);
		var actualChatId = ObjectId.Parse(chatGroupId);

		var user = Users.Find(x => x.Id == actualUserId).FirstOrDefault();
		if (user is null) return Result<string>.Failure(Errors.Accounts.UserNotFound);

		var group = Groups.Find(x => x.Id == actualChatId).FirstOrDefault();
		if (group is null) return Result<string>.Failure(Errors.ChatGroups.GroupNotFound);

		ChatMessage chatMessage = new() { UserId = user.Id, ChatGroupId = group.Id, Message = message };
		Messages.InsertOne(chatMessage);

		return Result<string>.Success(chatMessage.Id.ToString());
	}

	public Result<MessageData[]> GetMessages(string chatGroupId)
	{
		var actualChatId = ObjectId.Parse(chatGroupId);

		var group = Groups.Find(x => x.Id == actualChatId).FirstOrDefault();
		if (group is null) return Result<MessageData[]>.Failure(Errors.ChatGroups.GroupNotFound);

		var messages = Messages.Find(x => x.ChatGroupId == group.Id).ToList();
		var temp = new List<MessageData>();
		Dictionary<ObjectId, string> userNamesCache = new();
		foreach (var message in messages)
		{
			if (!userNamesCache.TryGetValue(message.UserId, out var userName))
			{
				userName = Users.Find(c => c.Id == message.UserId).FirstOrDefault()?.Name ?? string.Empty;
				userNamesCache.Add(message.UserId, userName);
			}

			temp.Add(new(userName, message.Message));
		}

		return Result<MessageData[]>.Success(temp.ToArray());
	}
}