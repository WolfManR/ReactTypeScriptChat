using ChatApi.Contracts;
using ChatApi.Data.Base;
using ChatApi.Results;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ChatApi.Data;

public class AccountsStorage : IAccountsStorage
{
	private readonly MongoDatabaseContext _context;

	public AccountsStorage(MongoDatabaseContext context) => _context = context;

	public async Task<User> GetOrAddUser(string name)
	{
		var user = await _context.Users.Find(x => x.Name == name).FirstOrDefaultAsync();
		if (user is not null)
		{
			return user;
		}

		user = new()
		{
			Name = name,
			Group = "user"
		};
		await _context.Users.InsertOneAsync(user);
		return user;
	}

	public Result<IEnumerable<ChatInfo>> GetUserChats(string userId)
	{
		if (ObjectId.TryParse(userId, out var actualUserId))
		{
			return Result<IEnumerable<ChatInfo>>.Failure(Errors.Accounts.NotValidUserId);
		}

		var chatInfoProjection = Builders<ChatGroup>.Projection.Expression<ChatInfo>(x => new(x.Name, x.Id.ToString(), GetChatLastMessage(x.Id)));

		var groups = _context.Groups.Find(x => x.Users.Contains(actualUserId)).Project(chatInfoProjection).ToList();
		return Result<IEnumerable<ChatInfo>>.Success(groups);
	}

	public async Task<Result<UserInfo>> GetUser(string userId)
	{
		if (ObjectId.TryParse(userId, out var actualUserId))
		{
			return Result<UserInfo>.Failure(Errors.Accounts.NotValidUserId);
		}

		var user = await _context.Users.Find(x => x.Id == actualUserId).FirstOrDefaultAsync();
		if (user is null) return Result<UserInfo>.Failure(Errors.Accounts.UserNotFound);

		UserInfo userInfo = new(user.Name);
		return Result<UserInfo>.Success(userInfo);
	}

	private string GetChatLastMessage(ObjectId id)
	{
		return _context.Messages.Find(t => t.ChatGroupId == id).SortByDescending(x => x.Created).FirstOrDefault()?.Message ?? string.Empty;
	}
}