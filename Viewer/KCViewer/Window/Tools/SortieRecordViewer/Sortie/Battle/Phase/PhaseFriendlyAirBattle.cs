using KCViewer.Core.Types.Data;
using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public class PhaseFriendlyAirBattle : PhaseAirBattleBase
{
	public override string Title => BattleRes.BattlePhaseFriendlyShelling;

	public PhaseFriendlyAirBattle(IKCDatabase kcDatabase, ApiKouku apiKouku)
		: base(kcDatabase, apiKouku)
	{
	}
}
