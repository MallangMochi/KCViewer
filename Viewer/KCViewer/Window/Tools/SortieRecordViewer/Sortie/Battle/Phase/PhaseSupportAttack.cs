using System.Collections.Generic;
using KCViewer.Core.Types;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public class PhaseSupportAttack
{
	public SupportType AttackType { get; init; }
	public List<PhaseSupportDefender> Defenders { get; init; } = new();
}
