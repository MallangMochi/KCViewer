using System.Collections.Generic;
using KCViewer.Database.KancolleApi;
using KCViewer.Database.Sortie;

namespace KCViewer.Database.Expedition;

public class ExpeditionRecord
{
	public int Id { get; set; }
	public int Expedition { get; set; }
	public List<ApiFile> ApiFiles { get; set; } = new();
	public SortieFleet Fleet { get; set; } = new();
}
