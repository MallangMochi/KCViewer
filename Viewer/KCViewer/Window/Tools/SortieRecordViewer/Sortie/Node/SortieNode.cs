using System.Collections.Generic;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Data;
using KCViewer.Data;
using KCViewer.KancolleApi.Types.ApiGetMember.ShipDeck;
using KCViewer.KancolleApi.Types.ApiReqMap.Models;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Node;

public abstract class SortieNode(
	IKCDatabase kcDatabase,
	int world,
	int map,
	int cell,
	CellType colorNo,
	int eventId,
	int eventKind)
{
	protected IKCDatabase KcDatabase { get; } = kcDatabase;

	public int World { get; } = world;
	public int Map { get; } = map;
	public int Cell { get; } = cell;
	public BattleBaseAirRaid? AirBaseRaid { get; private set; }
	public ApiHappening? Happening { get; set; }
	public List<ApiItemget?>? Items { get; set; }
	public ApiOffshoreSupply? ApiOffshoreSupply { get; set; }

	public CellType ApiColorNo { get; set; } = colorNo;
	public int ApiEventId { get; set; } = eventId;
	public int ApiEventKind { get; set; } = eventKind;

	public string DisplayCell => KCDatabase.Instance.Translation.Destination.CellDisplay(World, Map, Cell);

	public virtual IEnumerable<PhaseBase> AllPhases => AirBaseRaid?.Phases ?? [];

	public void AddAirBaseRaid(BattleBaseAirRaid abRaid)
	{
		AirBaseRaid = abRaid;
	}

	public virtual void UpdateState(ApiGetMemberShipDeckResponse deck)
	{

	}
}
