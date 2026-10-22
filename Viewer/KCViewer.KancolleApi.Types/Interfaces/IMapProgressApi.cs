using KCViewer.Core.Types;
using KCViewer.KancolleApi.Types.ApiReqMap.Models;

namespace KCViewer.KancolleApi.Types.Interfaces;

public interface IMapProgressApi
{
	/// <summary>
	/// Next cell ID
	/// </summary>
	int ApiNo { get; }
	CellType ApiColorNo { get; set; }
	int ApiEventId { get; set; }
	int ApiEventKind { get; set; }
	ApiHappening? ApiHappening { get; set; }
}
