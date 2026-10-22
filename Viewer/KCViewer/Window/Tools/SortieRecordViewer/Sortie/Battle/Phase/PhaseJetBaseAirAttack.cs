using System.Collections.Generic;
using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public class PhaseJetBaseAirAttack : PhaseBase
{
	public override string Title => BattleRes.BattlePhaseLandBasedJet;

	public List<PhaseBaseAirAttackUnit> Units { get; } = new();

	public PhaseJetBaseAirAttack(ApiAirBaseInjection apiAirBaseInjection)
	{
		Units.Add(new(apiAirBaseInjection, 0));
	}

	public override BattleFleets EmulateBattle(BattleFleets battleFleets)
	{
		FleetsBeforePhase = battleFleets.Clone();
		FleetsAfterPhase = battleFleets;

		foreach (PhaseBaseAirAttackUnit attackUnit in Units)
		{
			FleetsAfterPhase = attackUnit.EmulateBattle(FleetsAfterPhase);
		}

		return FleetsAfterPhase.Clone();
	}
}
