using KCViewer.KancolleApi.Types.Interfaces;

namespace KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EachBattle;

public class ApiReqCombinedBattleEachBattleRequest : IBattleApiRequest
{
	/// <inheritdoc />
	[JsonPropertyName("api_smoke_flag")]
	public string? ApiSmokeFlag { get; set; }
}
