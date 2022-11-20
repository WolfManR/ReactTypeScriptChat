using System.Security.Claims;

using ChatApi.Data;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Redis.OM;

var builder = WebApplication.CreateBuilder(args);

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

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("debug/database/initialize", async ([FromServices] DatabaseInitializer initializer) => await initializer.Initialize()).WithTags("Debug");
app.MapGet("debug/database/reinitialize", async ([FromServices] DatabaseInitializer initializer) => await initializer.Reinitialize()).WithTags("Debug");
app.MapGet("debug/accounts", async ([FromServices] ChatStorage storage) => Results.Ok(await storage.GetUsers())).WithTags("Debug");
app.MapGet("debug/groups", ([FromServices] ChatStorage storage) => Results.Ok(storage.GetChats())).WithTags("Debug");
app.MapGet("debug/messages", async ([FromServices] ChatStorage storage) => Results.Ok(await storage.GetMessages())).WithTags("Debug");

app.MapPost("auth/signin", async (string nick, HttpContext ctx, [FromServices] ChatStorage storage) =>
	{
		var user = await storage.SignIn(nick);

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

app.MapPost("auth/who-am-i", async (HttpContext ctx, [FromServices] ChatStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		var result = await storage.GetUser(userId);
		if (result.IsFailure) return Results.NotFound();
		return Results.Ok(result.Data!.Name);
	})
	.WithTags("Auth")
	.RequireAuthorization(fullEntryPolicy);

app.MapPost("groups/create", ([FromQuery] string name, HttpContext ctx, [FromServices] ChatStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		return Results.Ok(storage.CreateChatGroup(name, userId));
	})
	.WithTags("ChatGroups")
	.RequireAuthorization(userPolicy);

app.MapPost("groups/join", ([FromQuery] string chatGroupId, HttpContext ctx, [FromServices] ChatStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		return Results.Ok(storage.JoinGroup(userId, chatGroupId));
	})
	.WithTags("ChatGroups")
	.RequireAuthorization(userPolicy);

app.MapGet("groups", (HttpContext ctx, [FromServices] ChatStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		return Results.Ok(storage.GetUserChats(userId));
	})
	.WithTags("ChatGroups")
	.RequireAuthorization(userPolicy);

app.MapPost("messages/add", ([FromQuery] string chatGroupId, [FromQuery] string message, HttpContext ctx, [FromServices] ChatStorage storage) =>
	{
		var userId = ctx.User.FindFirstValue("usr");
		return Results.Ok(storage.AddMessage(userId, message, chatGroupId));
	})
	.WithTags("Messages")
	.RequireAuthorization(userPolicy);

app.MapGet("messages", ([FromQuery] string chatGroupId, [FromServices] ChatStorage storage) =>
	{
		return Results.Ok(storage.GetMessages(chatGroupId));
	})
	.WithTags("Messages")
	.RequireAuthorization(userPolicy);

app.Run();