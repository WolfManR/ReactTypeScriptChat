using ChatApi.Contracts;
using ChatApi.Data.Base;
using ChatApi.Results;

using MongoDB.Bson;
using MongoDB.Driver;

namespace ChatApi.Data;

public class ChatsStorage : IChatsStorage
{
	private readonly MongoDatabaseContext _context;

	public ChatsStorage(MongoDatabaseContext context) => _context = context;

	public Result<string> CreateChatGroup(string name, string userId)
	{
		if (ObjectId.TryParse(userId, out var actualUserId))
		{
			return Result<string>.Failure(Errors.Accounts.NotValidUserId);
		}

		var user = _context.Users.Find(x => x.Id == actualUserId).FirstOrDefault();
		if (user is null) return Result<string>.Failure(Errors.Accounts.UserNotFound);

		var group = new ChatGroup(name) { Created = DateTime.UtcNow }.AddUser(user);
		_context.Groups.InsertOne(group);

		return Result<string>.Success(group.Id.ToString());
	}

	public Result JoinGroup(string userId, string chatGroupId)
	{
		if (ObjectId.TryParse(userId, out var actualUserId))
		{
			return Result.Failure(Errors.Accounts.NotValidUserId);
		}
		if (ObjectId.TryParse(chatGroupId, out var actualChatId))
		{
			return Result.Failure(Errors.ChatGroups.NotValidChatId);
		}

		var user = _context.Users.Find(x => x.Id == actualUserId).FirstOrDefault();
		if (user is null) return Result.Failure(Errors.Accounts.UserNotFound);

		_context.Groups.FindOneAndUpdate(x => x.Id == actualChatId, Builders<ChatGroup>.Update.Push(x => x.Users, user.Id));

		return Result.Success();
	}

	public Result<string> AddMessage(string userId, string message, string chatGroupId)
	{
		if (ObjectId.TryParse(userId, out var actualUserId))
		{
			return Result<string>.Failure(Errors.Accounts.NotValidUserId);
		}
		if (ObjectId.TryParse(chatGroupId, out var actualChatId))
		{
			return Result<string>.Failure(Errors.ChatGroups.NotValidChatId);
		}

		var user = _context.Users.Find(x => x.Id == actualUserId).FirstOrDefault();
		if (user is null) return Result<string>.Failure(Errors.Accounts.UserNotFound);

		var group = _context.Groups.Find(x => x.Id == actualChatId).FirstOrDefault();
		if (group is null) return Result<string>.Failure(Errors.ChatGroups.GroupNotFound);

		ChatMessage chatMessage = new() { UserId = user.Id, ChatGroupId = group.Id, Message = message };
		_context.Messages.InsertOne(chatMessage);

		return Result<string>.Success(chatMessage.Id.ToString());
	}

	public Result<MessageData[]> GetMessages(string chatGroupId)
	{
		if (ObjectId.TryParse(chatGroupId, out var actualChatId))
		{
			return Result<MessageData[]>.Failure(Errors.ChatGroups.NotValidChatId);
		}

		var group = _context.Groups.Find(x => x.Id == actualChatId).FirstOrDefault();
		if (group is null) return Result<MessageData[]>.Failure(Errors.ChatGroups.GroupNotFound);

		var messages = _context.Messages.Find(x => x.ChatGroupId == group.Id).ToList();
		var temp = new MessageData[messages.Count];
		Dictionary<ObjectId, string> userNamesCache = new();
		for (var i = 0; i < messages.Count; i++)
		{
			var message = messages[i];
			if (!userNamesCache.TryGetValue(message.UserId, out var userName))
			{
				userName = _context.Users.Find(c => c.Id == message.UserId).FirstOrDefault()?.Name ?? string.Empty;
				userNamesCache.Add(message.UserId, userName);
			}

			temp[i] = new(userName, message.Message);
		}

		return Result<MessageData[]>.Success(temp);
	}
}