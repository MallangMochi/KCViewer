using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiGetMember.Mission;

public class ApiGetMemberMissionResponse
{
	[JsonPropertyName("api_limit_time")]
	public List<int> ApiLimitTime { get; set; } = new();

	[JsonPropertyName("api_list_items")]
	public List<ApiListItems> ApiListItems { get; set; } = new();
}
