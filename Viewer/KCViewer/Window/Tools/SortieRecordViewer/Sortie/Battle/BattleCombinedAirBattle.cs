using System.Collections.Generic;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.Airbattle;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle;

/// <summary>
/// 連合艦隊 vs 通常艦隊 航空戦 <br />
/// api_req_combined_battle/airbattle
/// </summary>
public sealed class BattleCombinedAirBattle : AirBattleData
{
	// Fork: the combined fleet's name (as Data/Battle/BattleCombinedAirBattle), not the normal fleet's
	public override string Title => ConstantsRes.Title_CombinedFleetAirBattle;

	private PhaseSupport? Support { get; }
	private PhaseAirBattle? AirBattle2 { get; }

	public BattleCombinedAirBattle(PhaseFactory phaseFactory, BattleFleets fleets, ApiReqCombinedBattleAirbattleResponse battle)
		: base(phaseFactory, fleets, battle)
	{
		Support = PhaseFactory.Support(battle.ApiSupportFlag, battle.ApiSupportInfo, false);
		AirBattle2 = PhaseFactory.AirBattle(battle.ApiKouku2, AirPhaseType.Second);

		EmulateBattle();
	}

	protected override IEnumerable<PhaseBase?> AllPhases()
	{
		yield return Initial;
		yield return Searching;
		yield return JetBaseAirAttack;
		yield return JetAirBattle;
		yield return BaseAirAttack;
		yield return FriendlySupportInfo;
		yield return FriendlyAirBattle;
		yield return AirBattle;
		yield return Support;
		yield return AirBattle2;
	}
}
