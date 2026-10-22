using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Avalonia.Dialogs.EquipmentSelector;
using KCViewer.Core.Types;
using EquipmentFilterResources = KCViewer.Avalonia.Controls.EquipmentFilter.EquipmentFilterResources;

namespace KCViewer.Fork.KcvViews;

/// <summary>An equipment of the selector: the viewer's row and what the table shows of it.</summary>
public sealed class KcvSelectorEquipment(EquipmentViewModel row)
{
	public EquipmentViewModel Row { get; } = row;
	public IEquipmentData Equipment => Row.Equipment;
	public EquipmentIconType Icon => Equipment.MasterEquipment.IconTypeTyped;
	public string Name => Equipment.MasterEquipment.NameEN;
	public int Level => Equipment.Level;
	public string LevelText => Level switch
	{
		<= 0 => "",
		>= 10 => "★max",
		_ => $"★+{Level}",
	};
	public int Proficiency => Equipment.AircraftLevel;
	public string ProficiencyText => Proficiency > 0 ? $"{Proficiency}" : "";
	public int Firepower => Equipment.MasterEquipment.Firepower;
	public int Torpedo => Equipment.MasterEquipment.Torpedo;
	public int AntiAir => Equipment.MasterEquipment.AA;
	public int Id => Equipment.EquipmentID;
}

/// <summary>
/// Fork: the viewer's equipment selector (개수 계획 관리's two adds, the plan editor's equipment, the upgrade tree's
/// assignments, the quest tracker's scrap condition) in KanColleViewer's style (mockup 20261009-upgrade-plan-windows-v2,
/// approved 2026-10-09 "구현"). The conditions are the equipment list's: the game's categories of the given equipment
/// with their icons (user's decision "필터 종류는 장비목록에서 쓴것처럼 인게임 분류를 사용") and the name, kept with the viewer's
/// selector so a selector opened again keeps them. At first all off as the viewer's (its groups all on, as the upgrade
/// tree's assignment sets them, turns them all on here too). Picking goes through the viewer's command, so its
/// DialogResult is set as the tree's assignment needs.
/// </summary>
public sealed class KcvEquipmentSelectorViewModel : ObservableObject
{
	private static ConditionalWeakTable<EquipmentSelectorViewModel, KcvEquipmentFilterViewModel> Filters { get; } = new();

	public EquipmentSelectorViewModel Selector { get; }
	public KcvEquipmentFilterViewModel Filter { get; }
	public List<KcvSelectorEquipment> Rows { get; }
	public ICollectionView View { get; }

	public bool ConditionsOpen { get; set; } = true;

	public int ShownCount { get; private set; }
	public string CountText => ForkStrings.KcvSelectorEquipmentCount(ShownCount);

	/// <summary>While folded, the conditions in use (mockup ③: 「적용 중: 소구경주포 · 장비명 「12.7」」).</summary>
	public string AppliedText { get; private set; } = "";
	public bool ShowApplied => !ConditionsOpen && AppliedText.Length > 0;

	private bool SettingAll { get; set; }

	public KcvEquipmentSelectorViewModel(EquipmentSelectorViewModel selector)
	{
		Selector = selector;

		Rows = selector.CollectionView.SourceCollection
			.OfType<EquipmentViewModel>()
			.Select(row => new KcvSelectorEquipment(row))
			.ToList();

		Filter = Filters.GetValue(selector, _ =>
		{
			KcvEquipmentFilterViewModel filter = new();
			filter.SetCategories(Rows.Select(row => row.Equipment.MasterEquipment));

			bool allOn = selector.EquipmentFilter.TypeFilters.All(group => group.IsChecked);
			foreach (KcvEquipmentCategoryFilter category in filter.Categories) category.IsChecked = allOn;
			filter.NameFilter = selector.EquipmentFilter.NameFilter ?? "";

			return filter;
		});

		View = CollectionViewSource.GetDefaultView(Rows);
		View.Filter = o => o is KcvSelectorEquipment row && Filter.Meets(row.Equipment.MasterEquipment);

		Filter.Changed += Refresh;
		PropertyChanged += OnChanged;
		Refresh();
	}

	/// <summary>전체 선택: all, none, or some categories.</summary>
	public bool? AllCategories
	{
		get => Filter.Categories.All(c => c.IsChecked) ? true : Filter.Categories.Any(c => c.IsChecked) ? null : false;
		set
		{
			SettingAll = true;
			foreach (KcvEquipmentCategoryFilter category in Filter.Categories) category.IsChecked = value is not false;
			SettingAll = false;

			Refresh();
		}
	}

	/// <summary>Picks through the viewer's command: it sets the selected equipment and the dialog result.</summary>
	public void Pick(KcvSelectorEquipment row)
	{
		Selector.SelectEquipmentCommand.Execute(row.Row);
	}

	public void Close()
	{
		Filter.Changed -= Refresh;
	}

	private void OnChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(ConditionsOpen)) OnPropertyChanged(nameof(ShowApplied));
	}

	private void Refresh()
	{
		if (SettingAll) return;

		View.Refresh();
		ShownCount = View.Cast<object>().Count();
		AppliedText = Applied();
		OnPropertyChanged(nameof(AllCategories));
		OnPropertyChanged(nameof(ShowApplied));
	}

	/// <summary>The conditions in use, in the order of the box: the categories when not all, the name.</summary>
	private string Applied()
	{
		List<string> conditions = [];

		List<KcvEquipmentCategoryFilter> on = [.. Filter.Categories.Where(c => c.IsChecked)];
		if (on.Count == 0) conditions.Add(ForkStrings.KcvSelectorNoCategory);
		else if (on.Count < Filter.Categories.Count) conditions.Add(string.Join(", ", on.Select(c => c.Name)));

		if (!string.IsNullOrEmpty(Filter.NameFilter)) conditions.Add($"{EquipmentFilterResources.Name} 「{Filter.NameFilter}」");

		return conditions.Count == 0 ? "" : ForkStrings.KcvSelectorApplied(conditions);
	}
}
