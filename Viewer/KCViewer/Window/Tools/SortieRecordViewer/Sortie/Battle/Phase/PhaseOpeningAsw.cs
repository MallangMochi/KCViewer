using KCViewer.Core.Types.Data;
using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public class PhaseOpeningAsw : PhaseShelling
{
	public override string Title => BattleRes.BattlePhaseOpeningAsw;

	public PhaseOpeningAsw(IKCDatabase kcDatabase, ApiHougeki1 shellingData)
		: base(kcDatabase, shellingData, DayShellingPhase.Other)
	{
	}
}
