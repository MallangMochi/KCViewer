using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiGetMember.ChartAdditionalInfo;

public class APIGetMemberChartAdditionalInfoResponse
{
	[JsonPropertyName("api_deck_param")]
	public List<ApiDeckParam> ApiDeckParam { get; set; } = [];
}
