using ChatApi.Contracts;
using ChatApi.Results;

namespace ChatApi.Data;

/// <summary> Storage that holds information about accounts and its data </summary>
public interface IAccountsStorage
{
	/// <summary> Search for user information by it name, if not find register new one </summary>
	/// <param name="name"> User name </param>
	/// <returns> User information </returns>
	Task<User> GetOrAddUser(string name);

	/// <summary> Search for user information by it id </summary>
	/// <param name="userId"> User information id </param>
	/// <returns> Result of operation with user information if it exist </returns>
	Task<Result<UserInfo>> GetUser(string userId);

	/// <summary> List chat groups which user joined </summary>
	/// <param name="userId"> User information id </param>
	/// <returns> List of chats </returns>
	Result<IEnumerable<ChatInfo>> GetUserChats(string userId);
}