using System.Security.Claims;

using ChatApi.Data;
using ChatApi.Data.Base;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Redis.OM;

var builder = WebApplication.CreateBuilder(args);

const string reactCORS = "react-app";
const string authenticationSchema = "Cookie";
const string userGroup = "user";
const string adminGroup = "Admin";
const string userPolicy = "user-policy";
const string adminPolicy = "admin-policy";
const string fullEntryPolicy = "all-can-access-policy";

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(authenticationSchema).AddCookie(authenticationSchema);

builder.Services.AddAuthorization(o =>
{
	o.AddPolicy(userPolicy, pb => pb.RequireAuthenticatedUser().AddAuthenticationSchemes(authenticationSchema).RequireClaim("group", userGroup));
	o.AddPolicy(adminPolicy, pb => pb.RequireAuthenticatedUser().AddAuthenticationSchemes(authenticationSchema).RequireClaim("group", adminGroup));
	o.AddPolicy(fullEntryPolicy, pb => pb.RequireAuthenticatedUser().AddAuthenticationSchemes(authenticationSchema).RequireClaim("group", adminGroup, userGroup));
});

builder.Services
	.AddScoped<IMongoClient, MongoClient>(p => new(p.GetRequiredService<IConfiguration>().GetConnectionString("Mongo")))
	.AddSingleton(new RedisConnectionProvider(builder.Configuration.GetConnectionString("Redis")))
	.AddScoped<MongoDatabaseContext>()
	.AddScoped<IDebugStorage, DebugStorage>()
	.AddScoped<IChatsStorage, ChatsStorage>()
	.AddScoped<IAccountsStorage, AccountsStorage>();

builder.Services.AddCors(options => options.AddPolicy(reactCORS, policyBuilder => policyBuilder.WithOrigins("http://localhost:5173").WithMethods("GET", "POST").AllowAnyHeader().AllowCredentials()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpLogging();
app.UseCors(reactCORS);


app.UseAuthentication();
app.UseAuthorization();

app.MapGet("debug/database/initialize", async ([FromServices] IDebugStorage debugStorage) => await debugStorage.SeedDatabase()).WithTags("Debug");
app.MapGet("debug/database/reinitialize", async ([FromServices] IDebugStorage debugStorage) =>
{
	await debugStorage.ClearDatabase();
	await debugStorage.SeedDatabase();
}).WithTags("Debug");
app.MapGet("debug/accounts", async ([FromServices] IDebugStorage storage) => Results.Ok(await storage.GetUsers())).WithTags("Debug");
app.MapGet("debug/groups", ([FromServices] IDebugStorage storage) => Results.Ok(storage.GetChats())).WithTags("Debug");
app.MapGet("debug/messages", async ([FromServices] IDebugStorage storage) => Results.Ok(await storage.GetMessages())).WithTags("Debug");

app.MapPost("auth/signin", async (string nick, HttpContext ctx, [FromServices] IAccountsStorage storage) =>
	{
		var user = await storage.GetOrAddUser(nick);

		ctx.Response.Cookies.Delete(".AspNetCore.Cookie");

		List<Claim> claims = new()
		{
			new("usr", user.Id.ToString()),
			new("group", user.Group)
		};
		ClaimsIdentity identity = new(claims, authenticationSchema);
		ClaimsPrincipal userPrincipal = new(identity);
		await ctx.SignInAsync(userPrincipal);
		return Results.Ok();
	})
	.WithTags("Auth")
	.AllowAnonymous();

app.MapPost("auth/what-is-my-group", (HttpContext ctx) =>
	{
		var group = ctx.User.FindFirstValue("group");
		return Results.Ok(group);
	})
	.WithTags("Auth")
	.RequireAuthorization(fullEntryPolicy);

app.MapPost("auth/who-am-i", async (HttpContext ctx, [FromServices] IAccountsStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		var result = await storage.GetUser(userId);
		if (result.IsFailure) return Results.NotFound();
		return Results.Ok(result.Data!.Name);
	})
	.WithTags("Auth")
	.RequireAuthorization(fullEntryPolicy);

app.MapGet("auth/signed-in", (HttpContext ctx) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		if (userId is null) return Results.Unauthorized();
		return Results.Ok();
	})
	.WithTags("Auth")
	.AllowAnonymous()
	.RequireCors(reactCORS);

app.MapPost("groups/create", ([FromQuery] string name, HttpContext ctx, [FromServices] IChatsStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		return Results.Ok(storage.CreateChatGroup(name, userId));
	})
	.WithTags("ChatGroups")
	.RequireAuthorization(userPolicy);

app.MapPost("groups/join", ([FromQuery] string chatGroupId, HttpContext ctx, [FromServices] IChatsStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		return Results.Ok(storage.JoinGroup(userId, chatGroupId));
	})
	.WithTags("ChatGroups")
	.RequireAuthorization(userPolicy);

app.MapGet("groups", (HttpContext ctx, [FromServices] IAccountsStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		return Results.Ok(storage.GetUserChats(userId));
	})
	.WithTags("ChatGroups")
	.RequireAuthorization(userPolicy);

app.MapPost("messages/add", ([FromQuery] string chatGroupId, [FromQuery] string message, HttpContext ctx, [FromServices] IChatsStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		return Results.Ok(storage.AddMessage(userId, message, chatGroupId));
	})
	.WithTags("Messages")
	.RequireAuthorization(userPolicy);

app.MapGet("messages", ([FromQuery] string chatGroupId, [FromServices] IChatsStorage storage) =>
	{
		return Results.Ok(storage.GetMessages(chatGroupId));
	})
	.WithTags("Messages")
	.RequireAuthorization(userPolicy);

app.Run();