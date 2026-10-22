using KCViewer.KancolleApi.Types.Interfaces;

namespace KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EcMidnightBattle;

public class ApiReqCombinedBattleEcMidnightBattleRequest : IBattleApiRequest
{
	[JsonPropertyName("api_verno")]
	public string ApiVerno { get; set; } = "";

	/// <inheritdoc />
	[JsonPropertyName("api_smoke_flag")]
	public string? ApiSmokeFlag { get; set; }
}
