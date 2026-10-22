using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiReqKaisou.SlotDeprive;

public class ApiReqKaisouSlotDepriveResponse
{
	[JsonPropertyName("api_ship_data")]
	public ApiShipData ApiShipData { get; set; } = new();

	[JsonPropertyName("api_unset_list")]
	public ApiUnsetList? ApiUnsetList { get; set; }
}
