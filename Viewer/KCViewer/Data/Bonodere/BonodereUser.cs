using System.Text.Json.Serialization;

namespace KCViewer.Data.Bonodere;

public class BonodereUser
{
	[JsonPropertyName("_id")]
	public required string Id { get; set; }

	[JsonPropertyName("info")]
	public required BonodereUserInfo UserInfo { get; set; }
}
