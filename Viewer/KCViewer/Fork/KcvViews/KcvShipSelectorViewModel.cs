using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Avalonia.Controls.ShipFilter;
using KCViewer.Avalonia.Dialogs.ShipSelector;
using KCViewer.Avalonia.Services;
using KCViewer.Core.Types;
using ShipFilterResources = KCViewer.Avalonia.Controls.ShipFilter.ShipFilterResources;

namespace KCViewer.Fork.KcvViews;

/// <summary>A ship of the selector: the viewer's row (its stats and equipment checks) and the banner.</summary>
public sealed class KcvSelectorShip(ShipViewModel row, ImageLoadService images)
{
	public IShipData Ship => row.Ship;
	public KcvPickerShip Banner { get; } = new(row.Ship.MasterShip, images);
	public string TypeName => Ship.MasterShip.ShipTypeName;
	public string Name => Ship.Name;
	public int Level => Ship.Level;
	public int Firepower => Ship.FirepowerBase;
	public int NightPower => row.NightPowerBase;
	public int Asw => Ship.ASWBase;
	public int Luck => Ship.LuckTotal;
	public bool Daihatsu => row.CanEquipDaihatsu;
	public bool Tank => row.CanEquipTank;
	public bool Fcf => row.CanEquipFcf;
	public bool Bulge => row.CanEquipBulge;
	public bool SeaplaneFighter => row.CanEquipSeaplaneFighter;
	public bool Expansion => row.IsExpansionSlotAvailable;
}

/// <summary>
/// Fork: the viewer's ship selector (칸무스 레벨링's 추가, the quest tracker's ship conditions, the Discord ship) in
/// KanColleViewer's style (mockup 20261008-ship-selector-v2, approved 2026-10-08 "구현"). The conditions are the
/// viewer's <see cref="ShipFilterViewModel"/>, so a selector opened again keeps them as the viewer's does, with a level
/// range (user's request) and the name searched as the other fork name boxes (<see cref="KcvNameSearch"/>). The rows are
/// the viewer's, in its order. Picking sets the viewer's <see cref="ShipSelectorViewModel.SelectedShip"/>.
/// </summary>
public sealed partial class KcvShipSelectorViewModel : ObservableObject
{
	/// <summary>The level range of a selector, kept with its filter as the viewer keeps the other conditions.</summary>
	private sealed class LevelRange
	{
		public int Min { get; set; }
		public int Max { get; set; } = 200;
	}

	private static ConditionalWeakTable<ShipFilterViewModel, LevelRange> Levels { get; } = new();

	[GeneratedRegex(@"\s*\([^)]*\)$")]
	private static partial Regex TypeCode();

	public ShipSelectorViewModel Selector { get; }
	public ShipFilterViewModel Filter => Selector.ShipFilter;
	public List<KcvSelectorShip> Ships { get; }
	public ICollectionView View { get; }

	public bool ConditionsOpen { get; set; } = true;
	public string NameFilter { get; set; }
	public int LevelMin { get; set; }
	public int LevelMax { get; set; }

	public int ShownCount { get; private set; }
	public string CountText => ForkStrings.KcvPickerShips(ShownCount);

	/// <summary>While folded, the conditions not at their first values (mockup ③: 「적용 중: 구축함 · 레벨 40~200」).</summary>
	public string AppliedText { get; private set; } = "";
	public bool ShowApplied => !ConditionsOpen && AppliedText.Length > 0;

	private LevelRange Level { get; }
	private bool SettingAllTypes { get; set; }

	public KcvShipSelectorViewModel(ShipSelectorViewModel selector)
	{
		Selector = selector;
		ImageLoadService images = Ioc.Default.GetRequiredService<ImageLoadService>();

		Ships = selector.CollectionView.SourceCollection
			.OfType<ShipViewModel>()
			.Select(row => new KcvSelectorShip(row, images))
			.ToList();
		View = CollectionViewSource.GetDefaultView(Ships);
		View.Filter = o => o is KcvSelectorShip ship && Matches(ship);

		// the name is searched here (also by the Japanese name), not by the viewer's filter
		NameFilter = Filter.NameFilter ?? "";
		Filter.NameFilter = null;

		Level = Levels.GetValue(Filter, _ => new());
		LevelMin = Level.Min;
		LevelMax = Level.Max;

		Filter.PropertyChanged += FollowFilter;
		PropertyChanged += OnChanged;
		Refresh();
	}

	/// <summary>전체 선택: all, none, or some groups.</summary>
	public bool? AllTypes
	{
		get => Filter.TypeFilters.All(f => f.IsChecked) ? true : Filter.TypeFilters.Any(f => f.IsChecked) ? null : false;
		set
		{
			SettingAllTypes = true;
			foreach (ShipTypeGroupFilterViewModel filter in Filter.TypeFilters)
			{
				filter.IsChecked = value is not false;
			}
			SettingAllTypes = false;

			OnPropertyChanged(nameof(AllTypes));
			Refresh();
		}
	}

	public void Pick(KcvSelectorShip ship)
	{
		Selector.SelectedShip = ship.Ship;
	}

	/// <summary>When the window closes: the name back into the viewer's filter, so it is there the next time.</summary>
	public void Close()
	{
		Filter.PropertyChanged -= FollowFilter;
		Filter.NameFilter = string.IsNullOrEmpty(NameFilter) ? null : NameFilter;
	}

	private bool Matches(KcvSelectorShip ship) =>
		Filter.MeetsFilterCondition(ship.Ship)
		&& ship.Level >= LevelMin
		&& ship.Level <= LevelMax
		&& (string.IsNullOrEmpty(NameFilter) || KcvNameSearch.Matches(ship.Ship.MasterShip, NameFilter));

	private void FollowFilter(object? sender, PropertyChangedEventArgs e)
	{
		if (SettingAllTypes) return;

		OnPropertyChanged(nameof(AllTypes));
		Refresh();
	}

	private void OnChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(NameFilter):
				Refresh();
				break;
			case nameof(LevelMin) or nameof(LevelMax):
				Level.Min = LevelMin;
				Level.Max = LevelMax;
				Refresh();
				break;
			case nameof(ConditionsOpen):
				OnPropertyChanged(nameof(ShowApplied));
				break;
		}
	}

	private void Refresh()
	{
		View.Refresh();
		ShownCount = View.Cast<object>().Count();
		AppliedText = Applied();
		OnPropertyChanged(nameof(ShowApplied));
	}

	/// <summary>The conditions not at their first values, in the order of the box.</summary>
	private string Applied()
	{
		List<string> conditions = [];

		List<ShipTypeGroupFilterViewModel> types = [.. Filter.TypeFilters.Where(f => f.IsChecked)];
		if (types.Count == 0) conditions.Add(ForkStrings.KcvSelectorNoType);
		else if (types.Count < Filter.TypeFilters.Count) conditions.Add(string.Join(", ", types.Select(f => TypeCode().Replace(f.Display, ""))));

		if (!string.IsNullOrEmpty(NameFilter)) conditions.Add($"{ShipFilterResources.Name} 「{NameFilter}」");
		if (Filter.FinalRemodel) conditions.Add(ShipFilterResources.FinalRemodel);

		void Range(string label, int min, int max)
		{
			if (min != 0 || max != 200) conditions.Add($"{label} {min}~{max}");
		}

		Range(ForkStrings.KcvSelectorLevel, LevelMin, LevelMax);
		Range(ShipFilterResources.Asw, Filter.AswMin, Filter.AswMax);
		Range(ShipFilterResources.Luck, Filter.LuckMin, Filter.LuckMax);

		void Equipment(string label, bool? state)
		{
			if (state is { } can) conditions.Add($"{label} {(can ? ForkStrings.KcvSelectorCan : ForkStrings.KcvSelectorCannot)}");
		}

		Equipment(ShipFilterResources.Daihatsu, Filter.CanEquipDaihatsu);
		Equipment(ShipFilterResources.Tank, Filter.CanEquipTank);
		Equipment(ShipFilterResources.Fcf, Filter.CanEquipFcf);
		Equipment(ShipFilterResources.Bulge, Filter.CanEquipBulge);
		Equipment(ShipFilterResources.SeaplaneFighter, Filter.CanEquipSeaplaneFighter);
		Equipment(ShipFilterResources.Expansion, Filter.IsExpansionSlotAvailable);

		return conditions.Count == 0 ? "" : ForkStrings.KcvSelectorApplied(conditions);
	}
}
