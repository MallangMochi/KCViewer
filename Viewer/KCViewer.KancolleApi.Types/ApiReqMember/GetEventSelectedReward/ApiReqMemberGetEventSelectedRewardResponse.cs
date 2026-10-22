using KCViewer.KancolleApi.Types.ApiReqMember.Models;

namespace KCViewer.KancolleApi.Types.ApiReqMember.GetEventSelectedReward;

public class ApiReqMemberGetEventSelectedRewardResponse
{
	[JsonPropertyName("api_get_item_list")]
	public List<ApiGetItemList> ApiGetItemList { get; set; } = new();
}
