using System.Collections.Generic;
using KCViewer.KancolleApi.Types.ApiReqMap.Models;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle;

/// <summary>
/// 基地防空戦 <br />
/// api_req_map/next
/// </summary>
public sealed class BattleBaseAirRaid : FirstBattleData
{
	public override string Title => ConstantsRes.Title_BaseAirRaid;

	private PhaseBaseAirRaid? BaseAirRaid { get; }

	public BattleBaseAirRaid(PhaseFactory phaseFactory, BattleFleets fleets, ApiDestructionBattle battle)
		: base(phaseFactory, fleets, battle)
	{
		BaseAirRaid = PhaseFactory.BaseAirRaid(battle);

		EmulateBattle();
	}

	protected override IEnumerable<PhaseBase?> AllPhases()
	{
		yield return Initial;
		yield return Searching;
		yield return BaseAirRaid;
	}
}
