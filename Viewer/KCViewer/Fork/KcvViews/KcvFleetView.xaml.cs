using System.Windows;
using System.Windows.Controls;
using KCViewer.Window.Wpf.Fleet;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: KanColleViewer's fleet design for <see cref="FleetViewModel"/> (see the XAML).</summary>
public partial class KcvFleetView : UserControl
{
	public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
		nameof(ViewModel), typeof(FleetViewModel), typeof(KcvFleetView), new PropertyMetadata(default(FleetViewModel), (d, _) => ((KcvFleetView)d).RefreshTimers()));

	public FleetViewModel ViewModel
	{
		get => (FleetViewModel)GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	public static readonly DependencyProperty DockHighlightProperty = DependencyProperty.Register(
		nameof(DockHighlight), typeof(bool), typeof(KcvFleetView), new PropertyMetadata(true));

	/// <summary>입거 시 강조: a docked ship's row in the docking colour (ForkSettings.FleetPanel), followed after the settings' OK.</summary>
	public bool DockHighlight
	{
		get => (bool)GetValue(DockHighlightProperty);
		set => SetValue(DockHighlightProperty, value);
	}

	/// <summary>The anchorage repair and home port supply timers in the header (KcvFleetTimers.cs).</summary>
	public KcvFleetTimersViewModel Timers { get; } = new();

	public KcvFleetView()
	{
		InitializeComponent();

		Loaded += (_, _) =>
		{
			Utility.SystemEvents.UpdateTimerTick += RefreshTimers;
			Utility.Configuration.Instance.ConfigurationChanged += ReadSettings;
			RefreshTimers();
			ReadSettings();
		};
		Unloaded += (_, _) =>
		{
			Utility.SystemEvents.UpdateTimerTick -= RefreshTimers;
			Utility.Configuration.Instance.ConfigurationChanged -= ReadSettings;
		};
	}

	private void ReadSettings()
	{
		DockHighlight = ForkSettings.Current.FleetPanel.DockHighlight;
	}

	private void RefreshTimers()
	{
		if (ViewModel is null) return;

		Timers.Refresh(ViewModel.FleetId);
	}

	private void OpenFleetDetail(object sender, RoutedEventArgs e)
	{
		if (ViewModel is null || System.Windows.Window.GetWindow(this) is not { } owner) return;

		KcvFleetDetailWindow.Open(ViewModel.FleetId, owner);
	}
}
