using ChatApi.Contracts;
using ChatApi.Results;

namespace ChatApi.Data;

/// <summary> Storage that holds information about chats and messages in its </summary>
public interface IChatsStorage
{
	/// <summary> Add message that write user in chat group </summary>
	/// <param name="userId"> User information id </param>
	/// <param name="message"> Message </param>
	/// <param name="chatGroupId"> Chat id </param>
	/// <returns> Message id </returns>
	Result<string> AddMessage(string userId, string message, string chatGroupId);

	/// <summary> Creates chat group </summary>
	/// <param name="name"> Name of chat group </param>
	/// <param name="userId"> User information id </param>
	/// <returns> Chat id </returns>
	Result<string> CreateChatGroup(string name, string userId);

	/// <summary> List of messages in chat group </summary>
	/// <param name="chatGroupId"> Chat id </param>
	/// <returns> Result of operation with List of messages </returns>
	Result<MessageData[]> GetMessages(string chatGroupId);

	/// <summary> Register user information in chat </summary>
	/// <param name="userId"> User information id </param>
	/// <param name="chatGroupId"> Chat id </param>
	/// <returns> Result of operation </returns>
	Result JoinGroup(string userId, string chatGroupId);
}