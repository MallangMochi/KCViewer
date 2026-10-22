using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Common;
using KCViewer.Observer;

namespace KCViewer.Fork.KcvViews;

/// <summary>An area button on the left.</summary>
public sealed class KcvAirAreaTab(int id, string title) : ObservableObject
{
	public int Id { get; } = id;
	public string Title { get; } = title;
	public bool IsSelected { get; set; }
}

/// <summary>
/// Fork: the land base details window (tools menu and the land base panel's button; mockup 5, user's approval
/// 2026-10-02), laid out as the fleet details window. Left: one button per area with land bases, the area's numbers
/// (bases, how many on each action, air defense power, high altitude), its state and the checks. Right: one row per
/// base (action and name, range with its bonus, the worst condition, fighter power, planes, its state, the four
/// squadrons). The values are <see cref="KcvAirBases"/>'s, as the panel shows them; every area is listed here.
/// </summary>
public sealed partial class KcvBaseAirCorpsDetailViewModel : WindowViewModelBase
{
	public string Title => ForkStrings.KcvAirBaseDetail;

	public ObservableCollection<KcvAirAreaTab> Tabs { get; } = [];

	public int AreaId { get; private set; }

	public KcvAirArea? Area { get; private set; }
	public bool HasArea { get; private set; }

	public string CorpsCount { get; private set; } = "";
	public string ActionSummary { get; private set; } = "";
	public string DefensePower { get; private set; } = "";
	public string HighAltitude { get; private set; } = "";
	public string StateText { get; private set; } = "";
	public Brush StateBrush { get; private set; } = KcvBrushes.NoShipBar;
	public List<KcvAirCheck> Checks { get; private set; } = [];
	public List<KcvAirCorps> Corps { get; private set; } = [];

	public KcvBaseAirCorpsDetailViewModel(int areaId)
	{
		AreaId = areaId;
		Update();
	}

	public override void Loaded()
	{
		base.Loaded();
		foreach (APIBase api in KcvAirBases.Apis(APIObserver.Instance)) api.ResponseReceived += OnApi;
	}

	public override void Closed()
	{
		base.Closed();
		foreach (APIBase api in KcvAirBases.Apis(APIObserver.Instance)) api.ResponseReceived -= OnApi;
	}

	private void OnApi(string apiName, dynamic data) => Update();

	[RelayCommand]
	public void SelectArea(int areaId)
	{
		AreaId = areaId;
		Update();
	}

	private void Update()
	{
		List<KcvAirArea> areas = KcvAirBases.Areas(false);

		if (!areas.Select(a => (a.AreaId, a.Title)).SequenceEqual(Tabs.Select(t => (t.Id, t.Title))))
		{
			Tabs.Clear();
			foreach (KcvAirArea a in areas) Tabs.Add(new KcvAirAreaTab(a.AreaId, a.Title));
		}

		// the asked area, else the first one (the bases may have moved)
		KcvAirArea? area = areas.FirstOrDefault(a => a.AreaId == AreaId) ?? areas.FirstOrDefault();
		AreaId = area?.AreaId ?? 0;
		foreach (KcvAirAreaTab tab in Tabs) tab.IsSelected = tab.Id == AreaId;

		Area = area;
		HasArea = area is not null;
		CorpsCount = area is null ? "" : ForkStrings.KcvAirCorpsCount(area.Corps.Count);
		ActionSummary = area?.ActionSummary ?? "";
		DefensePower = area is null ? "" : ForkStrings.KcvAirDefensePower(area.DefensePower);
		HighAltitude = area is null ? "" : ForkStrings.KcvAirHighAltitude(area.HighAltitude);
		StateText = area?.StateText ?? ForkStrings.KcvAirBaseNone;
		StateBrush = area?.StateBrush ?? KcvBrushes.NoShipBar;
		Checks = area?.Checks ?? [];
		Corps = area?.Corps ?? [];
	}
}
