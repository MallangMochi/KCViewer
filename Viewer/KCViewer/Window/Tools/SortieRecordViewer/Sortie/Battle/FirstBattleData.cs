using KCViewer.KancolleApi.Types.Interfaces;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle;

public abstract class FirstBattleData : BattleData
{
	protected PhaseSearching Searching { get; }

	protected FirstBattleData(PhaseFactory phaseFactory, BattleFleets fleets, IFirstBattleApiResponse battle)
		: base(phaseFactory, fleets, battle)
	{
		Searching = PhaseFactory.Searching(battle);
	}
}
