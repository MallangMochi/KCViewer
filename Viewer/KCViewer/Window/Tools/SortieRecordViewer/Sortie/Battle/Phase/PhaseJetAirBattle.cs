using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public class PhaseJetAirBattle : PhaseAirBattleBase
{
	public override string Title => BattleRes.BattlePhaseJet;

	public PhaseJetAirBattle(ApiInjectionKouku apiInjectionKouku) : base(apiInjectionKouku)
	{
		
	}
}
