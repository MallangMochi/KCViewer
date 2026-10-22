using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Database;
using KCViewer.Window.Dialog.QuestTrackerManager.Models;
using KCViewer.Window.Tools.DropRecordViewer;

namespace KCViewer.Window.Tools.AutoRefresh;

public partial class AutoRefreshRuleViewModel : ObservableObject
{
	public MapInfoModel Map { get; }

	public List<MapNode> AllCells { get; }
	public MapNode? SelectedCell { get; set; }

	public ObservableCollection<CellViewModel> AllowedCells { get; } = new();

	public bool IsEnabled { get; set; } = true;
	public int ManualCellId { get; set; } = 1;

	public AutoRefreshRuleViewModel(MapInfoModel map)
	{
		Map = map;

		string Grouping(int id) => Utility.Configuration.Config.UI.UseOriginalNodeId switch
		{
			true => $"{id}",
			_ => KCDatabase.Instance.Translation.Destination
				.CellDisplay(Map.AreaId, Map.InfoId, id)
		};

		using KCViewerContext db = new();

		AllCells = db
			.Cells
			.Where(c => c.CellType != CellType.Start)
			.Where(c => c.WorldId == Map.AreaId && c.MapId == Map.InfoId)
			.Select(r => r.CellId)
			.Distinct()
			.ToList()
			.GroupBy(Grouping)
			.Select(g => new MapNode(g.Key, g.OrderBy(i => i).ToList()))
			.ToList();

		SelectedCell = AllCells.FirstOrDefault();
	}

	[RelayCommand]
	private void AddAllowedCell()
	{
		if (SelectedCell?.Ids is null) return;

		foreach (int selectedCellId in SelectedCell.Ids)
		{
			AllowedCells.Add(new(selectedCellId));
		}
	}

	[RelayCommand]
	private void AddAllowedCellManual()
	{
		AllowedCells.Add(new(ManualCellId));
	}

	[RelayCommand]
	private void RemoveAllowedCell(CellViewModel? cell)
	{
		if (cell is null) return;

		AllowedCells.Remove(cell);
	}
}
