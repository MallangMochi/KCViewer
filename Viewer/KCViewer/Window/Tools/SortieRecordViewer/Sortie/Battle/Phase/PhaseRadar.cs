using KCViewer.Core.Types.Data;
using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public class PhaseRadar : PhaseShelling
{
	public PhaseRadar(IKCDatabase kcDatabase, ApiHougeki1 shellingData)
		: base(kcDatabase, shellingData, DayShellingPhase.First)
	{
	}
}
