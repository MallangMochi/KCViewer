using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiGetMember.PresetDeck;

public class ApiGetMemberPresetDeckResponse
{
	[JsonPropertyName("api_deck")]
	public IDictionary<string, ApiDeck> ApiDeck { get; set; } = new Dictionary<string, ApiDeck>();

	[JsonPropertyName("api_max_num")]
	public int ApiMaxNum { get; set; }
}
