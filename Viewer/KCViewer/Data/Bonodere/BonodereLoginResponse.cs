using System.Text.Json.Serialization;

namespace KCViewer.Data.Bonodere;

public class BonodereLoginResponse
{
	[JsonPropertyName("token")]
	public required string Token { get; set; }

	[JsonPropertyName("user")] 
	public required BonodereUser User { get; set; }
}
