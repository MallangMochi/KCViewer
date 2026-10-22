using KCViewer.KancolleApi.Types.ApiGetMember.Kdock;
using KCViewer.KancolleApi.Types.ApiReqKousyou.Models;
using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiReqKousyou.Getship;

public class ApiReqKousyouGetshipResponse
{
	[JsonPropertyName("api_id")]
	public int ApiId { get; set; }

	[JsonPropertyName("api_kdock")]
	public List<ApiGetMemberKdockResponse> ApiKdock { get; set; } = new();

	[JsonPropertyName("api_ship")]
	public ApiShip ApiShip { get; set; } = new();

	[JsonPropertyName("api_ship_id")]
	public int ApiShipId { get; set; }

	[JsonPropertyName("api_slotitem")]
	public List<ApiKousyouSlotitem> ApiSlotitem { get; set; } = new();
}
