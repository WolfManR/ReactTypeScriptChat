namespace ChatApi.Results;

public static class Errors
{
    public static class Authentication
    {
        public static readonly Error NotFound = new(401, "User not found");
        public static readonly Error Registered = new(403, "User already registered");
    }

    public static class Accounts
    {
        public static readonly Error NotValidUserId = new(22, "User Id not valid");
        public static readonly Error UserNotFound = new(23, "User not found");
    }

    public static class ChatGroups
	{
		public static readonly Error NotValidChatId = new(43, "Chat Id not valid");
		public static readonly Error GroupNotFound = new(44, "Group not found");
    }
}