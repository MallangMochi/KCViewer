using CommunityToolkit.Mvvm.ComponentModel;

namespace KCViewer.Window.Wpf.FleetPreset;

public class FleetPresetItemControlViewModel : ObservableObject
{
	public string? Text { get; set; }
	public string? ToolTip { get; set; }
}
