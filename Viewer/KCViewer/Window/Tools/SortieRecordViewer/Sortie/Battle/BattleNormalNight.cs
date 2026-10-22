using System.Collections.Generic;
using KCViewer.KancolleApi.Types.ApiReqBattleMidnight.Battle;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle;

/// <summary>
/// 通常艦隊 vs 通常艦隊 夜戦 <br />
/// api_req_battle_midnight/battle
/// </summary>
public sealed class BattleNormalNight : SecondNightBattleData
{
	public override string Title => ConstantsRes.Title_NormalNight;

	public BattleNormalNight(PhaseFactory phaseFactory, BattleFleets fleets, ApiReqBattleMidnightBattleResponse battle)
		: base(phaseFactory, fleets, battle)
	{
		EmulateBattle();
	}

	protected override IEnumerable<PhaseBase?> AllPhases()
	{
		yield return Initial;
		yield return NightInitial;
		yield return FriendlySupportInfo;
		yield return FriendlyShelling;
		yield return NightBattle;
	}
}
