using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Observer;

namespace KCViewer.Fork.KcvViews;

/// <summary>An area of the panel; the first one carries the details button (one button, user decision 2026-10-02).</summary>
public sealed record KcvAirAreaRow(KcvAirArea Area, bool First);

/// <summary>What the panel shows, rebuilt on the answers the viewer's land base panel updates on.</summary>
public sealed class KcvBaseAirCorpsPanelModel : ObservableObject
{
	public List<KcvAirAreaRow> Areas { get; private set; } = [];
	public bool HasNone { get; private set; } = true;
	public bool HasAreas { get; private set; }

	public void Update()
	{
		List<KcvAirArea> areas = KcvAirBases.Areas(Utility.Configuration.Config.FormBaseAirCorps.ShowEventMapOnly);
		Areas = areas.Select((area, i) => new KcvAirAreaRow(area, i == 0)).ToList();
		HasNone = areas.Count == 0;
		HasAreas = !HasNone;
	}
}

/// <summary>
/// Fork: the land base panel in KanColleViewer's style (see the XAML). The viewer's view model stays the panel's
/// DataContext for its context menu; the table has its own model.
/// </summary>
public partial class KcvBaseAirCorpsView
{
	public KcvBaseAirCorpsPanelModel Model { get; } = new();

	public KcvBaseAirCorpsView()
	{
		InitializeComponent();

		Loaded += (_, _) =>
		{
			foreach (APIBase api in KcvAirBases.Apis(APIObserver.Instance)) api.ResponseReceived += OnApi;
			Utility.Configuration.Instance.ConfigurationChanged += Model.Update;
			Model.Update();
		};
		Unloaded += (_, _) =>
		{
			foreach (APIBase api in KcvAirBases.Apis(APIObserver.Instance)) api.ResponseReceived -= OnApi;
			Utility.Configuration.Instance.ConfigurationChanged -= Model.Update;
		};
	}

	private void OnApi(string apiName, dynamic data) => Model.Update();

	private void OpenDetail(object sender, RoutedEventArgs e)
	{
		if (System.Windows.Window.GetWindow(this) is not { } owner) return;

		KcvBaseAirCorpsDetailWindow.Open(Model.Areas.FirstOrDefault()?.Area.AreaId ?? 0, owner);
	}
}
