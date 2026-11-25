using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Data;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: 자동 데이터 업데이트 수동 병합 (mockup 20261125-default-equipment-v6, approved "시안 6대로 구현"): the ships whose
/// recorded default equipment differs from a new version of the data (<see cref="ForkShipDefaultEquipment"/>), all
/// chosen at first; the chosen ones get the new data.
/// </summary>
public sealed class KcvDefaultEquipmentMergeViewModel : ObservableObject
{
	public string Header { get; }
	public string UpdatedText { get; }
	public string MatchedText { get; }
	public string ConflictsText { get; }
	public string NewColumn { get; }

	public IReadOnlyList<KcvDefaultEquipmentMergeRow> Rows { get; }

	public KcvDefaultEquipmentMergeViewModel(int version, int? previous, int updated, int matched, IEnumerable<KcvDefaultEquipmentMergeRow> rows)
	{
		Header = ForkStrings.DefaultEquipmentMergeHeader(version, previous);
		UpdatedText = ForkStrings.DefaultEquipmentMergeUpdated(updated);
		MatchedText = ForkStrings.DefaultEquipmentMergeMatched(matched);
		NewColumn = ForkStrings.DefaultEquipmentMergeNew(version);
		Rows = rows.ToList();
		ConflictsText = ForkStrings.DefaultEquipmentMergeConflicts(Rows.Count);

		foreach (KcvDefaultEquipmentMergeRow row in Rows)
		{
			row.PropertyChanged += WatchRowSelection;
		}
	}

	/// <summary>The header's check: all, none, or some (null) of the ships chosen; choosing it chooses all or none.</summary>
	public bool? AllSelected
	{
		get => Rows.All(r => r.IsSelected) ? true : Rows.Any(r => r.IsSelected) ? null : false;
		set
		{
			bool selected = value ?? false;
			foreach (KcvDefaultEquipmentMergeRow row in Rows)
			{
				row.IsSelected = selected;
			}
		}
	}

	public int SelectedCount => Rows.Count(r => r.IsSelected);

	public string ApplyText => ForkStrings.DefaultEquipmentMergeApply(SelectedCount);

	public bool CanApply => SelectedCount > 0;

	public IReadOnlyCollection<int> SelectedShips => Rows.Where(r => r.IsSelected).Select(r => r.ShipId).ToList();

	private void WatchRowSelection(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(KcvDefaultEquipmentMergeRow.IsSelected)) return;

		OnPropertyChanged(nameof(AllSelected));
		OnPropertyChanged(nameof(SelectedCount));
		OnPropertyChanged(nameof(ApplyText));
		OnPropertyChanged(nameof(CanApply));
	}
}

/// <summary>A ship of the window: the profile's record and the new data, slot by slot, the slots that differ marked.</summary>
public sealed class KcvDefaultEquipmentMergeRow : ObservableObject
{
	private bool isSelected = true;

	public int ShipId { get; }
	public string Name { get; }
	public string Detail { get; }
	public IReadOnlyList<KcvDefaultEquipmentSlot> Profile { get; }
	public IReadOnlyList<KcvDefaultEquipmentSlot> New { get; }

	public bool IsSelected
	{
		get => isSelected;
		set => SetProperty(ref isSelected, value);
	}

	public KcvDefaultEquipmentMergeRow(int shipId, int[] profile, int[] data)
	{
		ShipId = shipId;

		var ship = KCDatabase.Instance.MasterShips[shipId];
		Name = ship?.NameWithClass ?? shipId.ToString();
		Detail = ForkStrings.DefaultEquipmentMergeShipDetail(shipId, ship?.ShipTypeName ?? "");

		// as many slots as the ship has, and any filled beyond them
		int shown = new[] { ship?.SlotSize ?? 0, LastFilled(profile) + 1, LastFilled(data) + 1, 1 }.Max();
		Profile = Enumerable.Range(0, shown).Select(i => new KcvDefaultEquipmentSlot(i + 1, profile[i], profile[i] != data[i])).ToList();
		New = Enumerable.Range(0, shown).Select(i => new KcvDefaultEquipmentSlot(i + 1, data[i], profile[i] != data[i])).ToList();
	}

	private static int LastFilled(int[] slots) => System.Array.FindLastIndex(slots, id => id != -1);
}

/// <summary>A slot: its number and equipment (none, or the name), gold when the record and the data differ there.</summary>
public sealed class KcvDefaultEquipmentSlot(int number, int equipment, bool differs)
{
	public int Number { get; } = number;
	public bool Differs { get; } = differs;
	public bool IsEmpty { get; } = equipment == -1;

	public string Name { get; } = equipment == -1
		? ForkStrings.DefaultEquipmentMergeEmptySlot
		: KCDatabase.Instance.MasterEquipments[equipment]?.NameEN ?? ForkStrings.ShipParamUnknown;
}
