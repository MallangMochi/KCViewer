using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiGetMember.SortieConditions;

public class ApiGetMemberSortieConditionsResponse
{
	[JsonPropertyName("api_war")]
	public ApiWar ApiWar { get; set; } = new();
}
