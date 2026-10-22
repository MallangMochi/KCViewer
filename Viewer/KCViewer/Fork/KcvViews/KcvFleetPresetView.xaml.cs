using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Data;
using KCViewer.Observer;

namespace KCViewer.Fork.KcvViews;

/// <summary>What the preset panel shows: rebuilt when the presets change (as the viewer's panel) and at the port.</summary>
public sealed class KcvFleetPresetPanelModel : ObservableObject
{
	public List<KcvPresetRow> Rows { get; private set; } = [];
	public bool HasNone { get; private set; } = true;
	public bool HasRows { get; private set; }
	public bool ShowSeventh { get; private set; }

	public void Update()
	{
		List<KcvPresetRow>? rows = KcvFleetPresets.Rows();
		Rows = rows ?? [];
		HasNone = rows is null;
		HasRows = !HasNone;
		ShowSeventh = Rows.FirstOrDefault()?.ShowSeventh ?? false;
	}
}

/// <summary>Fork: the fleet preset panel in KanColleViewer's style (see the XAML).</summary>
public partial class KcvFleetPresetView
{
	public KcvFleetPresetPanelModel Model { get; } = new();

	public KcvFleetPresetView()
	{
		InitializeComponent();

		// the viewer's panel follows only the preset answers; the port answer also brings the ships' condition and level
		Loaded += (_, _) =>
		{
			KCDatabase.Instance.FleetPreset.PresetChanged += Model.Update;
			APIObserver.Instance.ApiPort_Port.ResponseReceived += OnPort;
			Utility.Configuration.Instance.ConfigurationChanged += Model.Update;
			Model.Update();
		};
		Unloaded += (_, _) =>
		{
			KCDatabase.Instance.FleetPreset.PresetChanged -= Model.Update;
			APIObserver.Instance.ApiPort_Port.ResponseReceived -= OnPort;
			Utility.Configuration.Instance.ConfigurationChanged -= Model.Update;
		};
	}

	private void OnPort(string apiName, dynamic data) => Model.Update();
}
