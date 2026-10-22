using KCViewer.KancolleApi.Types.ApiReqAirCorps.Models;

namespace KCViewer.KancolleApi.Types.ApiReqAirCorps.ChangeDeploymentBase;

public class ApiReqAirCorpsChangeDeploymentBaseResponse
{
	[JsonPropertyName("api_base_items")]
	public List<ApiBaseItem> ApiBaseItems { get; set; } = new();
}
