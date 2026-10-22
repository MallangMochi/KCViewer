using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Resource;

namespace KCViewer.Window.Wpf.BaseAirCorps;

public class BaseAirCorpsItemControlViewModel : ObservableObject
{
	public string? Text { get; set; }
	public bool Visible { get; set; }
	public IconContent? SupplyIcon { get; set; }
	public IconContent? ConditionIcon { get; set; }
	public int Tag { get; set; }
	public string? ToolTip { get; set; }
}
