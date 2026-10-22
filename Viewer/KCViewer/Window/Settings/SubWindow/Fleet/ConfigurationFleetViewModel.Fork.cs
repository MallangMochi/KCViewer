using KCViewer.Fork;

namespace KCViewer.Window.Settings.SubWindow.Fleet;

/// <summary>
/// Fork: the fleet panels' own settings (<see cref="ForkSettings.FleetPanelSettings"/>) in the KanColleViewer style
/// settings window's 보조 윈도우 › 함대 › 표시 (mockup 20261009-fleet-dock-badge-v5): 입거 시 강조 right under 대파 시 강조,
/// 편성 변경 시 패널 자동 전환 at the end.
/// </summary>
public partial class ConfigurationFleetViewModel
{
	public bool ForkDockHighlight { get; set; } = ForkSettings.Current.FleetPanel.DockHighlight;
	public bool ForkSwitchOnComposition { get; set; } = ForkSettings.Current.FleetPanel.SwitchOnComposition;

	public string ForkDockHighlightLabel => ForkStrings.KcvFleetDockHighlight;
	public string ForkDockHighlightToolTip => ForkStrings.KcvFleetDockHighlightToolTip;
	public string ForkSwitchOnCompositionLabel => ForkStrings.KcvFleetSwitchOnComposition;
	public string ForkSwitchOnCompositionToolTip => ForkStrings.KcvFleetSwitchOnCompositionToolTip;

	private void ForkSave()
	{
		ForkSettings.FleetPanelSettings panel = ForkSettings.Current.FleetPanel;
		if (panel.DockHighlight == ForkDockHighlight && panel.SwitchOnComposition == ForkSwitchOnComposition) return;

		panel.DockHighlight = ForkDockHighlight;
		panel.SwitchOnComposition = ForkSwitchOnComposition;
		ForkSettings.Current.Save();
	}
}
