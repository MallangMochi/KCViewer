using System.Text.Json.Serialization;

namespace KCViewer.Data.Bonodere;

public class BonodereUserInfo
{
	[JsonPropertyName("name")]
	public required string Username { get; set; }
}
