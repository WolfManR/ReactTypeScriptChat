using System.Collections;

namespace ChatApi.Data;

/// <summary> Storage that holds debug information and help work with it </summary>
public interface IDebugStorage
{
	/// <summary> Seed database with data </summary>
	/// <returns> <see cref="ValueTask"/> </returns>
	ValueTask SeedDatabase();

	/// <summary> Clear database from all it data </summary>
	/// <returns> <see cref="ValueTask"/> </returns>
	ValueTask ClearDatabase();

	/// <summary> List all chats in database </summary>
	/// <returns >List of chats </returns>
	Task<IEnumerable> GetChats();


	/// <summary> List all messages in database </summary>
	/// <returns> List of messages </returns>
	Task<IEnumerable> GetMessages();


	/// <summary> List all users in database </summary>
	/// <returns> List of users </returns>
	Task<IEnumerable> GetUsers();
}