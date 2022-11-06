using ChatApi.Contracts;
using ChatApi.Results;
using Redis.OM;
using Redis.OM.Searching;

namespace ChatApi.Data;

class ChatStorage
{
    private readonly RedisConnectionProvider _provider;
    private IRedisCollection<User>? _users;
    private IRedisCollection<ChatGroup>? _groups;
    private IRedisCollection<ChatMessage>? _messages;

    public ChatStorage(RedisConnectionProvider provider) => _provider = provider;

    private IRedisCollection<User> Users => _users ??= _provider.RedisCollection<User>();
    private IRedisCollection<ChatGroup> Groups => _groups ??= _provider.RedisCollection<ChatGroup>();
    private IRedisCollection<ChatMessage> Messages => _messages ??= _provider.RedisCollection<ChatMessage>();

    public Result<string> SignIn(string name)
    {
        // broken search
        // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
        var user = Users.Where(x => x.Name == name).FirstOrDefault();
        return user is not null
            ? Result<string>.Success(user.Id.ToString())
            : Result<string>.Failure(Errors.Authentication.NotFound);
    }

    public Result<string> SignUp(string name)
    {
        // broken search
        // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
        var user = Users.Where(x => x.Name == name).FirstOrDefault();
        if (user is not null) return Result<string>.Failure(Errors.Authentication.Registered);

        var id = Users.Insert(new() { Name = name });
        return Result<string>.Success(id);
    }

    public UserInfo[] GetUsers()
    {
        var users = Users.ToList();
        List<UserInfo> usersInfo = new();

        foreach (var user in users)
        {
            // not parsing group id correctly
            //var groups = Groups
            //    .Where(c => c.Users.Contains(userId))
            //    .Select(k => new { k.Name , k.Id }).ToArray();
            var groups = Groups.ToList()
                .Where(c => c.Users.Contains(user.Id))
                .Select(x => new ChatInfo(x.Name, x.Id.ToString(), GetChatLastMessage(x.Id)))
                .ToArray();
            usersInfo.Add(new(user.Name, user.Id.ToString(), groups));
        }

        return usersInfo.ToArray();
    }

    private string GetChatLastMessage(Ulid id)
    {
        return Messages.Where(t => t.ChatGroupId == id).ToList().LastOrDefault()?.Message ?? string.Empty;
    }

    public ChatInfo[] GetChats()
    {
        var groups = Groups.ToList()
            .Select(c => new ChatInfo(c.Name, c.Id.ToString(), GetChatLastMessage(c.Id)))
            .ToArray();
        return groups;
    }

    public MessageInfo[] GetMessages()
    {
        var messages = new List<MessageInfo>();
        foreach (var x in Messages.ToList())
        {
            var userName = Users.FindById(x.UserId.ToString())?.Name ?? string.Empty;
            var chatGroup = Groups.FindById(x.ChatGroupId.ToString())?.Name ?? string.Empty;
            messages.Add(new(userName, x.Message, chatGroup));
        }

        return messages.ToArray();
    }

    public Result<string> CreateChatGroup(string name, string userId)
    {
        var user = Users.FindById(userId);
        if (user is null) return Result<string>.Failure(Errors.Accounts.UserNotFound);

        var group = new ChatGroup(name).AddUser(user);
        string id = Groups.Insert(group);

        return Result<string>.Success(id);
    }

    public Result JoinGroup(string userId, string chatGroupId)
    {
        var user = Users.FindById(userId);
        if (user is null) return Result.Failure(Errors.Accounts.UserNotFound);

        var group = Groups.FindById(chatGroupId);
        if (group is null) return Result.Failure(Errors.ChatGroups.GroupNotFound);

        group.AddUser(user);
        Groups.Update(group);

        return Result.Success();
    }

    public Result<string> AddMessage(string userId, string message, string chatGroupId)
    {
        var user = Users.FindById(userId);
        if (user is null) return Result<string>.Failure(Errors.Accounts.UserNotFound);

        var group = Groups.FindById(chatGroupId);
        if (group is null) return Result<string>.Failure(Errors.ChatGroups.GroupNotFound);

        ChatMessage chatMessage = new() { UserId = user.Id, ChatGroupId = group.Id, Message = message };
        string id = Messages.Insert(chatMessage);

        return Result<string>.Success(id);
    }

    public Result<MessageData[]> GetMessages(string chatGroupId)
    {
        var group = Groups.FindById(chatGroupId);
        if (group is null) return Result<MessageData[]>.Failure(Errors.ChatGroups.GroupNotFound);

        var messages = Messages.Where(x=>x.ChatGroupId == group.Id).ToList();
        var temp = new List<MessageData>();
        Dictionary<Ulid, string> userNamesCache = new();
        foreach (var message in messages)
        {
            if (!userNamesCache.TryGetValue(message.UserId, out var userName))
            {
                userName = Users.FindById(message.UserId.ToString())?.Name ?? string.Empty;
                userNamesCache.Add(message.UserId, userName);
            }
            
            temp.Add(new(userName, message.Message));
        }

        return Result<MessageData[]>.Success(temp.ToArray());
    }
}