using System;
using KCViewer.Core;
using KCViewer.Core.Types;
using KCViewer.Utility.Mathematics;
using KCViewer.Window.Dialog.QuestTrackerManager.Enums;

namespace KCViewer.Window.Tools.DropRecordViewer;

public class DropRecordRow : DropRecordRowBase
{
	public required int Index { get; init; }
	public required string Name { get; init; }
	public required DateTime Date { get; init; }
	public required string MapDescription { get; init; }
	public required BattleRank Rank { get; init; }
	public required ShipId ShipId { get; set; }

	public string? DateDisplay =>  DateTimeHelper.TimeToCSVString(Date);

	// Fork: the record itself, for the KanColleViewer style drop table (node letter, enemy fleet; Fork/KcvViews)
	public Resource.Record.ShipDropRecord.ShipDropElement? ForkElement { get; init; }
}
