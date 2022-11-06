namespace ChatApi.Contracts;

public record UserInfo(string Name, string Id, ChatInfo[] Chats);