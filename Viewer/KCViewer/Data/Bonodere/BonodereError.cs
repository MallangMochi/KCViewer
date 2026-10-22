using System.Text.Json.Serialization;

namespace KCViewer.Data.Bonodere;

public class BonodereError
{
	[JsonPropertyName("message")]
	public string Message { get; set; } = "";

	[JsonPropertyName("code")]
	public int Code { get; set; }
}
