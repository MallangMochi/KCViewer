using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core.Types;
using KCViewer.Window.Dialog.QuestTrackerManager.Enums;
using MessagePack;

namespace KCViewer.Window.Dialog.QuestTrackerManager.Models.Conditions;

[MessagePackObject]
public class ShipTypeConditionModel : ObservableObject, ICondition
{
	[Key(0)] public ObservableCollection<ShipTypes> Types { get; set; } = [ShipTypes.Destroyer];
	[Key(1)] public int Count { get; set; }
	[Key(2)] public bool MustBeFlagship { get; set; }
	[Key(3)] public ComparisonType ComparisonType { get; set; } = ComparisonType.GreaterOrEqual;
	[Key(4)] public int Level { get; set; }
}
