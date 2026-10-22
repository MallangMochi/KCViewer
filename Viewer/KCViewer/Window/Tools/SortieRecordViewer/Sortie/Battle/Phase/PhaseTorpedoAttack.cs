using System.Collections.Generic;
using KCViewer.Core.Types.Attacks;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public record PhaseTorpedoAttack
{
	public BattleIndex Attacker { get; init; }
	public DayAttackKind AttackType { get; init; }
	public List<PhaseShellingDefender> Defenders { get; init; } = new();
}
