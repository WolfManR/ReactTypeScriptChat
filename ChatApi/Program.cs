using Microsoft.AspNetCore.Mvc;
using Redis.OM;
using Redis.OM.Modeling;
using Redis.OM.Searching;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddSingleton(new RedisConnectionProvider(builder.Configuration.GetConnectionString("Redis")))
    .AddScoped<DatabaseInitializer>()
    .AddScoped<ChatStorage>();

builder.Services.AddCors(options => options.AddPolicy("react-app", policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("react-app");

app.MapGet("database/initialize", async ([FromServices] DatabaseInitializer initializer) => await initializer.Initialize()).WithTags("Debug");
app.MapGet("database/reinitialize", async ([FromServices] DatabaseInitializer initializer) => await initializer.Reinitialize()).WithTags("Debug");
app.MapGet("accounts", ([FromServices] ChatStorage storage) => Results.Ok(storage.GetUsers())).WithTags("Debug");
app.MapGet("groups", ([FromServices] ChatStorage storage) => Results.Ok(storage.GetChats())).WithTags("Debug");
app.MapGet("messages", ([FromServices] ChatStorage storage) => Results.Ok(storage.GetMessages())).WithTags("Debug");

app.MapPost("auth/signin", (string nick, [FromServices] ChatStorage storage) => Results.Ok(storage.SignIn(nick))).WithTags("Auth");
app.MapPost("auth/signup", (string nick, [FromServices] ChatStorage storage) => Results.Ok(storage.SignUp(nick))).WithTags("Auth");

app.MapPost("groups/create", ([FromQuery] string name, [FromQuery] string userId, [FromServices] ChatStorage storage)
        => Results.Ok(storage.CreateChatGroup(name, userId)))
    .WithTags("ChatGroups");
app.MapPost("groups/join", ([FromQuery] string userId, [FromQuery] string chatGroupId, [FromServices] ChatStorage storage)
        => Results.Ok(storage.JoinGroup(userId, chatGroupId)))
    .WithTags("ChatGroups");

app.MapPost("messages/add", ([FromQuery] string userId, [FromQuery] string chatGroupId, [FromQuery] string message, [FromServices] ChatStorage storage)
        => Results.Ok(storage.AddMessage(userId, message, chatGroupId)))
    .WithTags("Messages");
app.MapGet("messages", ([FromQuery] string chatGroupId, [FromServices] ChatStorage storage)
        => Results.Ok(storage.GetMessages(chatGroupId)))
    .WithTags("Messages");

app.Run();

class DatabaseInitializer
{
    private readonly RedisConnectionProvider _provider;

    private static readonly Type[] Documents =
    {
        typeof(User),
        typeof(ChatGroup),
        typeof(ChatMessage)
    };

    public DatabaseInitializer(RedisConnectionProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask Initialize()
    {
        foreach (var document in Documents)
            await _provider.Connection.CreateIndexAsync(document);
    }

    public async ValueTask Reinitialize()
    {
        var indexesDrop = Documents.Select(x => _provider.Connection.DropIndexAndAssociatedRecords(x)).ToList();
        if (indexesDrop.All(x => x)) await Initialize();
    }
}

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

public static class Errors
{
    public static class Authentication
    {
        public static readonly Error NotFound = new(401, "User not found");
        public static readonly Error Registered = new(403, "User already registered");
    }

    public static class Accounts
    {
        public static readonly Error UserNotFound = new(23, "User not found");
    }

    public static class ChatGroups
    {
        public static readonly Error GroupNotFound = new(44, "Group not found");
    }
}

public record struct Error(int Code, string Message);
public record Result(int Code)
{
    public bool IsFailure => Code > 0;
    public string? Message { get; init; }

    public static Result Success() => new(0);
    public static Result Failure(Error error) => new(error.Code) { Message = error.Message };
}
public record Result<T>(int Code, T? Data = default) : Result(Code)
{
    public static Result<T> Success(T data) => new(0, data);
    public new static Result<T> Failure(Error error) => new(error.Code) { Message = error.Message };
}

class Entity
{
    [RedisIdField] public Ulid Id { get; set; }
}

class Named : Entity
{
    [Searchable] public string Name { get; set; } = null!;
}

[Document(StorageType = StorageType.Json)]
class User : Named
{

}

[Document(StorageType = StorageType.Json)]
class ChatGroup : Named
{
    public ChatGroup() { }
    public ChatGroup(string name) => Name = name;

    [Indexed] public List<Ulid> Users { get; init; } = new();

    public ChatGroup AddUser(User user)
    {
        if (!Users.Contains(user.Id))
        {
            Users.Add(user.Id);
        }

        return this;
    }
}

[Document(StorageType = StorageType.Json)]
class ChatMessage : Entity
{
    public string Message { get; set; } = null!;
    [Indexed] public Ulid ChatGroupId { get; init; }
    [Indexed] public Ulid UserId { get; init; }
}

public record UserInfo(string Name, string Id, ChatInfo[] Chats);
public record ChatInfo(string Name, string Id, string LastMessage);
public record MessageData(string UserName, string Message);
public record MessageInfo(string UserName, string Message, string ChatGroupName);