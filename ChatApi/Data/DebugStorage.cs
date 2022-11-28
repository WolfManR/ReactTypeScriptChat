using System.Collections;
using ChatApi.Data.Base;
using MongoDB.Driver;

namespace ChatApi.Data;

public class DebugStorage : IDebugStorage
{
	private readonly MongoDatabaseContext _context;

	public DebugStorage(MongoDatabaseContext context) => _context = context;

	public async ValueTask SeedDatabase()
	{
		if (await _context.Users.CountDocumentsAsync(x => true) == 0)
		{
			await _context.Users.InsertOneAsync(new() { Group = "Admin", Name = "Admin" });
		}
	}

	public async ValueTask ClearDatabase()
	{
		await _context.Users.DeleteManyAsync(x => true).ConfigureAwait(false);

		await _context.Groups.DeleteManyAsync(x => true);

		await _context.Messages.DeleteManyAsync(x => true);
	}

	async Task<IEnumerable> IDebugStorage.GetUsers()
	{
		var cursor = await _context.Users.FindAsync(x => true);
		List<object> usersInfo = new();

		var chatInfoProjection = Builders<ChatGroup>.Projection.Expression<object>(x => new { x.Name, id = x.Id.ToString() });

		foreach (var user in cursor.ToEnumerable())
		{
			var groups = _context.Groups.Find(x => x.Users.Contains(user.Id))
				.Project(chatInfoProjection)
				.ToList();
			usersInfo.Add(new { name = user.Name, id = user.Id.ToString(), chats = groups.ToArray() });
		}

		return usersInfo.ToArray();
	}

	async Task<IEnumerable> IDebugStorage.GetChats()
	{
		var chatInfoProjection = Builders<ChatGroup>.Projection.Expression<object>(x => new { x.Name, id = x.Id.ToString() });

		var groups = await _context.Groups.Find(x => true)
			.Project(chatInfoProjection)
			.ToListAsync();
		return groups;
	}

	async Task<IEnumerable> IDebugStorage.GetMessages()
	{
		var messages = new List<object>();
		var messagesCursor = await _context.Messages.Find(x => true).ToCursorAsync();

		foreach (var x in messagesCursor.ToEnumerable())
		{
			var userName = _context.Users.Find(c => c.Id == x.UserId).FirstOrDefault()?.Name ?? string.Empty;
			var chatGroup = _context.Groups.Find(c => c.Id == x.ChatGroupId).FirstOrDefault()?.Name ?? string.Empty;
			messages.Add(new { userName, x.Message, chatGroup });
		}

		return messages;
	}
}