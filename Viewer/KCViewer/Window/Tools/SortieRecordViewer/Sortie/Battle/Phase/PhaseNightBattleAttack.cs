using System.Collections.Generic;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Attacks;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public record PhaseNightBattleAttack
{
	public BattleIndex Attacker { get; init; }
	public bool NightAirAttackFlag { get; set; }
	public NightAttackKind AttackType { get; init; }
	public List<IEquipmentDataMaster> DisplayEquipments { get; set; }
	public List<PhaseNightBattleDefender> Defenders { get; init; } = new();
}
