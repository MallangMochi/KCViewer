using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Core.Types;
using KCViewer.Data;

namespace KCViewer.Fork.KcvViews;

/// <summary>A check box of the filter: a game equipment category (api_type[2]) with its usual icon.</summary>
public sealed class KcvEquipmentCategoryFilter(int id, string name, EquipmentIconType icon) : ObservableObject
{
	public int Id { get; } = id;
	public string Name { get; } = name;
	public EquipmentIconType Icon { get; } = icon;
	public bool IsChecked { get; set; } = true;
}

/// <summary>
/// Fork: the equipment filter of the KanColleViewer designs (user decision 2026-09-30): one check box per game
/// equipment category (小口径主砲, 中口径主砲, 艦上戦闘機, ...; names from the game data as the viewer translates
/// them), each with the icon most equipment of that category has. Only categories of owned equipment are shown.
/// The name box matches like the viewer's own filter and also by the Japanese name (<see cref="KcvNameSearch"/>).
/// </summary>
public sealed partial class KcvEquipmentFilterViewModel : ObservableObject
{
	public ObservableCollection<KcvEquipmentCategoryFilter> Categories { get; } = [];

	public string? NameFilter { get; set; } = "";

	/// <summary>Raised when the user changes the filter (not when <see cref="SetCategories"/> refreshes the list).</summary>
	public event Action? Changed;

	private bool Refreshing { get; set; }

	public KcvEquipmentFilterViewModel()
	{
		PropertyChanged += (_, args) =>
		{
			if (args.PropertyName is nameof(NameFilter)) Changed?.Invoke();
		};
	}

	/// <summary>Shows the categories of this equipment, in the game's order; the check state of each category stays.</summary>
	public void SetCategories(IEnumerable<IEquipmentDataMaster> owned)
	{
		List<IEquipmentDataMaster> masters = owned.ToList();
		List<int> ids = masters.Select(eq => (int)eq.CategoryType).Distinct().Order().ToList();
		if (ids.SequenceEqual(Categories.Select(c => c.Id))) return;

		Dictionary<int, bool> checkedBefore = Categories.ToDictionary(c => c.Id, c => c.IsChecked);

		Refreshing = true;
		Categories.Clear();
		foreach (int id in ids)
		{
			IEquipmentDataMaster first = masters.First(eq => (int)eq.CategoryType == id);
			KcvEquipmentCategoryFilter filter = new(id, first.CategoryTypeInstance.NameEN, IconOf(id))
			{
				IsChecked = checkedBefore.GetValueOrDefault(id, true),
			};
			filter.PropertyChanged += (_, _) =>
			{
				if (!Refreshing) Changed?.Invoke();
			};
			Categories.Add(filter);
		}
		Refreshing = false;
	}

	public bool Meets(IEquipmentDataMaster equipment)
	{
		int category = (int)equipment.CategoryType;
		if (Categories.FirstOrDefault(c => c.Id == category) is { IsChecked: false }) return false;
		if (!string.IsNullOrEmpty(NameFilter) && !KcvNameSearch.Matches(equipment, NameFilter)) return false;

		return true;
	}

	/// <summary>All on, or all off when all are on (like the viewer's type toggle).</summary>
	[RelayCommand]
	private void ToggleAll()
	{
		bool check = !Categories.All(c => c.IsChecked);

		Refreshing = true;
		foreach (KcvEquipmentCategoryFilter filter in Categories) filter.IsChecked = check;
		Refreshing = false;

		Changed?.Invoke();
	}

	/// <summary>The icon most player equipment of a category has.</summary>
	private static EquipmentIconType IconOf(int category) => KCDatabase.Instance.MasterEquipments.Values
		.Where(eq => !eq.IsAbyssalEquipment && (int)eq.CategoryType == category)
		.GroupBy(eq => eq.IconTypeTyped)
		.OrderByDescending(icons => icons.Count())
		.Select(icons => icons.Key)
		.FirstOrDefault();
}
