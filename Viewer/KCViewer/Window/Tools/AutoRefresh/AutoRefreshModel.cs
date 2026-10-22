using System.Collections.Generic;
using KCViewer.Database.MapData;

namespace KCViewer.Window.Tools.AutoRefresh;

public class AutoRefreshModel
{
	public int Id { get; set; }
	public bool IsSingleMapMode { get; set; }
	public MapModel SingleMapModeMap { get; set; } = new();
	public List<AutoRefreshRuleModel> Rules { get; set; } = new();
}
