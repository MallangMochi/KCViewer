using KCViewer.KancolleApi.Types.ApiReqBattleMidnight.Battle;
using KCViewer.KancolleApi.Types.ApiReqBattleMidnight.SpMidnight;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.Battle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.BattleWater;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EachBattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EachBattleWater;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EcBattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EcMidnightBattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.LdAirbattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.LdShooting;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.MidnightBattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.SpMidnight;
using KCViewer.KancolleApi.Types.ApiReqMap.Models;
using KCViewer.KancolleApi.Types.ApiReqSortie.Airbattle;
using KCViewer.KancolleApi.Types.ApiReqSortie.Battle;
using KCViewer.KancolleApi.Types.ApiReqSortie.LdAirbattle;
using KCViewer.KancolleApi.Types.ApiReqSortie.LdShooting;
using KCViewer.KancolleApi.Types.Legacy.OpeningTorpedoRework;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle;

public class BattleFactory(PhaseFactory phaseFactory)
{
	private PhaseFactory PhaseFactory { get; } = phaseFactory;

	public BattleNormalDay CreateBattle(ApiReqSortieBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleNormalNight CreateBattle(ApiReqBattleMidnightBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleNightOnly CreateBattle(ApiReqBattleMidnightSpMidnightResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedEachDay CreateBattle(ApiReqCombinedBattleEachBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleEnemyCombinedDay CreateBattle(ApiReqCombinedBattleEcBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleEnemyCombinedNight CreateBattle(ApiReqCombinedBattleEcMidnightBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedEachWater CreateBattle(ApiReqCombinedBattleEachBattleWaterResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleAirBattle CreateBattle(ApiReqSortieAirbattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleAirRaid CreateBattle(ApiReqSortieLdAirbattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedAirRaid CreateBattle(ApiReqCombinedBattleLdAirbattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedNightOnly CreateBattle(ApiReqCombinedBattleSpMidnightResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleBaseAirRaid CreateBattle(ApiDestructionBattle battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedNormalDay CreateBattle(ApiReqCombinedBattleBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedWater CreateBattle(ApiReqCombinedBattleBattleWaterResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedNormalNight CreateBattle(ApiReqCombinedBattleMidnightBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleNormalRadar CreateBattle(ApiReqSortieLdShootingResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedRadar CreateBattle(ApiReqCombinedBattleLdShootingResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleNormalDay CreateBattle(OpeningTorpedoRework_ApiReqSortieBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedNormalDay CreateBattle(OpeningTorpedoRework_ApiReqCombinedBattleBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedWater CreateBattle(OpeningTorpedoRework_ApiReqCombinedBattleBattleWaterResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleEnemyCombinedDay CreateBattle(OpeningTorpedoRework_ApiReqCombinedBattleEcBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedEachDay CreateBattle(OpeningTorpedoRework_ApiReqCombinedBattleEachBattleResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);

	public BattleCombinedEachWater CreateBattle(OpeningTorpedoRework_ApiReqCombinedBattleEachBattleWaterResponse battle, BattleFleets fleets)
		=> new(PhaseFactory, fleets, battle);
}
