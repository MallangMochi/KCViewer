using KCViewer.KancolleApi.Types.Interfaces;

namespace KCViewer.KancolleApi.Types.ApiReqCombinedBattle.Airbattle;

public class ApiReqCombinedBattleAirbattleRequest : IBattleApiRequest
{
	[JsonPropertyName("api_verno")]
	public string ApiVerno { get; set; } = "";

	/// <inheritdoc />
	[JsonPropertyName("api_smoke_flag")]
	public string? ApiSmokeFlag { get; set; }
}
