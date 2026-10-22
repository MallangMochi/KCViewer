using KCViewer.Core.Types;
using KCViewer.Core.Types.Attacks;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public class SupportAttack
{
	public IShipData Defender { get; set; }
	public SupportType AttackKind { get; set; }
	public int Damage { get; set; }
	public bool GuardsFlagship { get; set; }
	public HitType CriticalFlag { get; set; }
}
