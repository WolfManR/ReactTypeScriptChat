using ChatApi.Data;
using Microsoft.AspNetCore.Mvc;
using Redis.OM;

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