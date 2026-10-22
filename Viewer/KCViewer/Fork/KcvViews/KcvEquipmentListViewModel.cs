using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Common;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.ViewModels;
using KCViewer.ViewModels.Translations;

namespace KCViewer.Fork.KcvViews;

/// <summary>Who holds an equipment: a ship ("夕雲改", "Lv.36"), a land base or equipment being relocated.</summary>
public sealed record KcvEquipmentHolder(string Name, string Level, int Count)
{
	public string CountText => Count > 1 ? $" ×{Count}" : "";
}

/// <summary>One improvement level of an equipment: ★+n (and plane proficiency), owned, spare and holders.</summary>
public sealed record KcvEquipmentLevel(int Level, int AircraftLevel, int Count, int Spare, List<KcvEquipmentHolder> Holders, bool IsFirst)
{
	public string LevelText => $"★+{Level}";
}

/// <summary>An equipment type with its total and its improvement levels.</summary>
public sealed record KcvEquipmentGroup(
	int Id,
	int Category,
	int AlbumNo,
	EquipmentIconType IconType,
	string Name,
	int Total,
	string ToolTip,
	List<KcvEquipmentLevel> Levels)
{
	/// <summary>The owned count of the improvement level with the most (what the owned header sorts by).</summary>
	public int MostOwned => Levels.Max(level => level.Count);
}

/// <summary>What the list is sorted by (header clicks). None: the list's own order, category then equipment id.</summary>
public enum KcvEquipmentSort
{
	None,
	Name,
	Total,
	Owned,
}

/// <summary>
/// Fork: the equipment list as KanColleViewer's 所有装備一覧 (viewer-design/kcv-reference/03-equipment-list.png): one
/// table with a row per equipment type, split by improvement level, each with the ships holding it. The counting
/// follows the viewer's own list (EquipmentListViewModel): ships, land bases and relocated equipment hold equipment.
/// The viewer's type filter and "locked only" stay.
/// Sorting (user's choice 2026-10-02, mockup 2): the name, total and owned headers. The name sorts by album number
/// from the first, the numbers from the largest; a click on the sorted column turns it round. Owned is the level with
/// the most, an equipment's lines move together, and equal values keep the list's own order. Kept through refreshes
/// and filter changes; a new window starts in the list's own order.
/// </summary>
public sealed partial class KcvEquipmentListViewModel : WindowViewModelBase
{
	public DialogEquipmentListTranslationViewModel DialogEquipmentList { get; }

	public KcvEquipmentFilterViewModel Filters { get; } = new();

	public bool ShowLockedEquipmentOnly { get; set; }

	/// <summary>The categories unfolded, kept in the settings (mockup 20261005-list-search-v2, user decision 2026-10-05).</summary>
	public bool FiltersExpanded { get; set; } = ForkSettings.Current.KcvStyle.EquipmentListFiltersExpanded;

	/// <summary>While folded with a category off: 「적용 중: 분류 14/18」 (categories of the owned equipment).</summary>
	public string AppliedText => FiltersExpanded || Filters.Categories.All(c => c.IsChecked)
		? ""
		: ForkStrings.KcvFiltersApplied([ForkStrings.KcvAppliedCategories(Filters.Categories.Count(c => c.IsChecked), Filters.Categories.Count)]);

	public List<KcvEquipmentGroup> Groups { get; private set; } = [];

	public KcvEquipmentSort SortColumn { get; private set; }
	public bool SortDescending { get; private set; }

	// The arrow of each sortable header: "Descending", "Ascending", or "" when another column (or none) is sorted
	public string NameSort => SortColumn != KcvEquipmentSort.Name ? "" : SortDescending ? "Descending" : "Ascending";
	public string TotalSort => SortColumn != KcvEquipmentSort.Total ? "" : SortDescending ? "Descending" : "Ascending";
	public string OwnedSort => SortColumn != KcvEquipmentSort.Owned ? "" : SortDescending ? "Descending" : "Ascending";

	public string Title => DialogEquipmentList.Title;

	public KcvEquipmentListViewModel()
	{
		DialogEquipmentList = Ioc.Default.GetRequiredService<DialogEquipmentListTranslationViewModel>();

		PropertyChanged += (_, args) =>
		{
			if (args.PropertyName is nameof(ShowLockedEquipmentOnly)) Update();

			if (args.PropertyName is nameof(FiltersExpanded))
			{
				ForkSettings.Current.KcvStyle.EquipmentListFiltersExpanded = FiltersExpanded;
				ForkSettings.Current.Save();
				OnPropertyChanged(nameof(AppliedText));
			}
		};
		Filters.Changed += Update;

		Update();
	}

	[RelayCommand]
	private void Update()
	{
		KCDatabase db = KCDatabase.Instance;

		// Holder of each equipment, by the equipment's own id
		Dictionary<int, (string Name, string Level)> holders = new();
		foreach (ShipData ship in db.Ships.Values)
		{
			foreach (IEquipmentData? equipment in ship.AllSlotInstance)
			{
				if (equipment is not null) holders[equipment.MasterID] = (ship.Name, $"Lv.{ship.Level}");
			}
		}

		foreach (BaseAirCorpsData corps in db.BaseAirCorps.Values)
		{
			foreach (IBaseAirCorpsSquadron? squadron in corps.Squadrons.Values)
			{
				if (squadron?.EquipmentInstance is { } equipment) holders[equipment.MasterID] = ($"#{corps.MapAreaID} {corps.Name}", "");
			}
		}

		foreach (RelocationData relocation in db.RelocatedEquipments.Values)
		{
			if (relocation.EquipmentInstance is { } equipment) holders[equipment.MasterID] = (ForkStrings.KcvRelocating, "");
		}

		List<EquipmentData> owned = db.Equipments.Values
			.OfType<EquipmentData>()
			.Where(equipment => (!ShowLockedEquipmentOnly || equipment.IsLocked))
			.ToList();
		Filters.SetCategories(owned.Select(equipment => equipment.MasterEquipment));

		Groups = Sorted(owned
			.Where(equipment => Filters.Meets(equipment.MasterEquipment))
			.GroupBy(equipment => equipment.EquipmentID)
			.Select(type =>
			{
				IEquipmentDataMaster master = type.First().MasterEquipment;
				List<KcvEquipmentLevel> levels = type
					.GroupBy(equipment => (equipment.Level, equipment.AircraftLevel))
					.OrderBy(level => level.Key.Level)
					.ThenBy(level => level.Key.AircraftLevel)
					.Select((level, index) => new KcvEquipmentLevel(
						level.Key.Level,
						level.Key.AircraftLevel,
						level.Count(),
						level.Count(equipment => !holders.ContainsKey(equipment.MasterID)),
						level
							.Where(equipment => holders.ContainsKey(equipment.MasterID))
							.GroupBy(equipment => holders[equipment.MasterID])
							.Select(holder => new KcvEquipmentHolder(holder.Key.Name, holder.Key.Level, holder.Count()))
							.ToList(),
						index == 0))
					.ToList();

				return new KcvEquipmentGroup(
					master.EquipmentID,
					(int)master.CategoryType,
					master.AlbumNo,
					master.IconTypeTyped,
					master.NameEN,
					type.Count(),
					$"{master.CategoryTypeInstance.NameEN} {master.NameEN} (ID: {master.EquipmentID})",
					levels);
			}));

		// every filter change and refresh comes here (the categories can change with the owned equipment)
		OnPropertyChanged(nameof(AppliedText));
	}

	/// <summary>A header click: the other direction on the sorted column, else its first direction.</summary>
	[RelayCommand]
	private void Sort(KcvEquipmentSort column)
	{
		SortDescending = column == SortColumn ? !SortDescending : column != KcvEquipmentSort.Name;
		SortColumn = column;
		Groups = Sorted(Groups);
	}

	/// <summary>The list's own order first, so equal values keep it (OrderBy is stable).</summary>
	private List<KcvEquipmentGroup> Sorted(IEnumerable<KcvEquipmentGroup> groups)
	{
		List<KcvEquipmentGroup> own = groups
			.OrderBy(group => group.Category)
			.ThenBy(group => group.Id)
			.ToList();

		Func<KcvEquipmentGroup, int>? key = SortColumn switch
		{
			// album number; the same as the equipment id for every equipment (master data 2026-10-02)
			KcvEquipmentSort.Name => group => group.AlbumNo,
			KcvEquipmentSort.Total => group => group.Total,
			KcvEquipmentSort.Owned => group => group.MostOwned,
			_ => null,
		};
		if (key is null) return own;

		return (SortDescending ? own.OrderByDescending(key) : own.OrderBy(key)).ToList();
	}

	[RelayCommand]
	private void OpenEquipmentEncyclopedia(int id)
	{
		KcvAlbums.ShowEquipment(id);
	}
}
