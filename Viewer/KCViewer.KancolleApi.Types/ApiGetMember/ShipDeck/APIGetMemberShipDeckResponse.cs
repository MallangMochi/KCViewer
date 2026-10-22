using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiGetMember.ShipDeck;

public class ApiGetMemberShipDeckResponse
{
	[JsonPropertyName("api_deck_data")]
	public List<FleetDataDto> ApiDeckData { get; set; } = new();

	[JsonPropertyName("api_ship_data")]
	public List<ApiShip> ApiShipData { get; set; } = new();
}
