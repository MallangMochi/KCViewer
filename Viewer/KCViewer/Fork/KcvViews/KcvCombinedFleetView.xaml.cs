using System.Windows;
using System.Windows.Controls;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the combined fleet panel for <see cref="KcvCombinedFleetViewModel"/> (see the XAML).</summary>
public partial class KcvCombinedFleetView : UserControl
{
	public static readonly DependencyProperty DockHighlightProperty = DependencyProperty.Register(
		nameof(DockHighlight), typeof(bool), typeof(KcvCombinedFleetView), new PropertyMetadata(true));

	/// <summary>입거 시 강조: a docked ship's row in the docking colour (ForkSettings.FleetPanel), as the fleet panel.</summary>
	public bool DockHighlight
	{
		get => (bool)GetValue(DockHighlightProperty);
		set => SetValue(DockHighlightProperty, value);
	}

	public KcvCombinedFleetView()
	{
		InitializeComponent();

		Loaded += (_, _) =>
		{
			Utility.Configuration.Instance.ConfigurationChanged += ReadSettings;
			ReadSettings();
		};
		Unloaded += (_, _) => Utility.Configuration.Instance.ConfigurationChanged -= ReadSettings;
	}

	private void ReadSettings()
	{
		DockHighlight = ForkSettings.Current.FleetPanel.DockHighlight;
	}
}
