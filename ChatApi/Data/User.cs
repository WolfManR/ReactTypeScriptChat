using Redis.OM.Modeling;

namespace ChatApi.Data;

[Document(StorageType = StorageType.Json)]
class User : Named
{
	public string Group { get; set; } = null!;
}