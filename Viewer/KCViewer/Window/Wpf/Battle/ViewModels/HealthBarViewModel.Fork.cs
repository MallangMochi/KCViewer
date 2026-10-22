using System.Globalization;

namespace KCViewer.Window.Wpf.Battle.ViewModels;

/// <summary>
/// Fork: the ship of the bar by name and ship type, for the KanColleViewer battle panel (Fork/KcvViews/KcvBattleView.xaml),
/// which shows them as its BattleInfo does; the viewer's own panel shows the short type and a tooltip only.
/// Set by BattleViewModel.ForkSetBattle.
/// </summary>
public partial class HealthBarViewModel
{
	public string? ShipName { get; set; }

	public string? ShipTypeName { get; set; }

	/// <summary>
	/// The HP lost in this battle over the gauge ("-12"; mockup 20261013-battle-hp-damage-v5), as the viewer counts it:
	/// from the HP the battle (or the night battle) started with, below 0 for a ship sunk by more than it had.
	/// Nothing when no HP was lost.
	/// </summary>
	public string? ForkDamageText => Value < PrevValue ? (Value - PrevValue).ToString(CultureInfo.InvariantCulture) : null;
}
