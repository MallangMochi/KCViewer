using KCViewer.KancolleApi.Types.Interfaces;

namespace KCViewer.KancolleApi.Types.ApiReqCombinedBattle.MidnightBattle;

public class ApiReqCombinedBattleMidnightBattleRequest : IBattleApiRequest
{
	/// <inheritdoc />
	[JsonPropertyName("api_smoke_flag")]
	public string? ApiSmokeFlag { get; set; }
}
