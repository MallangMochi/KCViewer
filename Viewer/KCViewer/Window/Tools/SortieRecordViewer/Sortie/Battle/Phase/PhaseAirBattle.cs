using KCViewer.Core.Types.Data;
using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public class PhaseAirBattle(IKCDatabase kcDatabase, ApiKouku airBattleData, AirPhaseType type)
	: PhaseAirBattleBase(kcDatabase, airBattleData)
{
	private AirPhaseType Type { get; } = type;

	public override string Title => Type switch
	{
		AirPhaseType.First => BattleRes.BattlePhaseAirAttackFirst,
		AirPhaseType.Second => BattleRes.BattlePhaseAirAttackSecond,
		AirPhaseType.Raid => BattleRes.BattlePhaseAirRaid,
		_ => BattleRes.BattlePhaseAirBattle,
	};
}
