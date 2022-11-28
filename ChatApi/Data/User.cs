using ChatApi.Data.Base;

namespace ChatApi.Data;

public class User : Named
{
	public string Group { get; set; } = null!;
}