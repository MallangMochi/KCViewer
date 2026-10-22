using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiReqHokyu.Charge;

public class ApiReqHokyuChargeResponse
{
	[JsonPropertyName("api_material")]
	public List<int> ApiMaterial { get; set; } = new();

	[JsonPropertyName("api_ship")]
	public List<ApiChargeShip> ApiShip { get; set; } = new();

	[JsonPropertyName("api_use_bou")]
	public int ApiUseBou { get; set; }
}
