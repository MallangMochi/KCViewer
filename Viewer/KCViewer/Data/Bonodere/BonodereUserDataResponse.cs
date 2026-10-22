using System.Text.Json.Serialization;

namespace KCViewer.Data.Bonodere;

public class BonodereUserDataResponse
{
	[JsonPropertyName("data")] 
	public required BonodereUser User { get; set; }
}
