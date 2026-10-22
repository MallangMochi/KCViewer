using System.ComponentModel;

namespace KCViewer.Avalonia.Behaviors.PersistentColumns;

public class SortDescriptionModel
{
	public required string PropertyPath { get; init; }
	public required ListSortDirection Direction { get; init; }
}
