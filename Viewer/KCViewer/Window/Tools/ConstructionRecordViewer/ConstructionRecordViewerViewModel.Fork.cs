using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Core.Types;

namespace KCViewer.Window.Tools.ConstructionRecordViewer;

/// <summary>
/// Fork: the [검색] [취소] buttons of the KanColleViewer style window (Fork/KcvViews/KcvConstructionRecordWindow), as
/// in DevelopmentRecordViewerViewModel.Fork.cs.
/// </summary>
public partial class ConstructionRecordViewerViewModel
{
	public bool ForkIsSearching { get; private set; }
	public bool ForkCanSearch => !ForkIsSearching;
	private bool ForkWatchesSearcher { get; set; }

	[RelayCommand]
	private void ForkSearch()
	{
		if (Searcher.IsBusy) return;

		if (!ForkWatchesSearcher)
		{
			Searcher.RunWorkerCompleted += (_, _) => ForkIsSearching = false;
			ForkWatchesSearcher = true;
		}

		ForkIsSearching = true;
		StartSearchCommand.Execute(null);
	}

	[RelayCommand]
	private void ForkCancelSearch()
	{
		if (Searcher.IsBusy) Searcher.CancelAsync();
	}

	private List<object>? ForkAllShips { get; set; }

	/// <summary>
	/// The ship list follows the category: only that ship type's ships ("건조기록에서 카테고리 함명이 갱신되지 않음",
	/// 2026-10-05; the viewer filters the flagships by their type the same way). A chosen ship that is not in the new
	/// list goes back to (모두).
	/// </summary>
	public void ForkFollowCategory() => PropertyChanged += (_, e) =>
	{
		if (e.PropertyName is not nameof(SelectedShipType)) return;

		ForkAllShips ??= Ships;
		Ships = SelectedShipType switch
		{
			ShipTypes type => ForkAllShips.Where(s => s is not IShipDataMaster ship || ship.ShipType == type).ToList(),
			_ => ForkAllShips,
		};
		if (!Ships.Contains(SelectedShip)) SelectedShip = ConstructionRecordOption.All;
	};
}
