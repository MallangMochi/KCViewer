using KCViewer.KancolleApi.Types.ApiGetMember.Unsetslot;
using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiGetMember.Ship3;

public class ApiGetMemberShip3Response
{
	[JsonPropertyName("api_deck_data")]
	public List<FleetDataDto> ApiDeckData { get; set; } = [];

	[JsonPropertyName("api_ship_data")]
	public List<ApiShip> ApiShipData { get; set; } = [];

	[JsonPropertyName("api_slot_data")]
	public ApiGetMemberUnsetslotResponse ApiSlotData { get; set; } = [];
}
