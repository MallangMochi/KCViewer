using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core.Types;
using KCViewer.Window.Dialog.QuestTrackerManager.Enums;
using MessagePack;

namespace KCViewer.Window.Dialog.QuestTrackerManager.Models.Conditions;

[MessagePackObject]
public class ShipPositionConditionModel : ObservableObject, ICondition
{
	[Key(0)] public ShipId Id { get; set; } = ShipId.Kamikaze;
	[Key(1)] public int Position { get; set; } = 1;
	[Key(2)] public RemodelComparisonType RemodelComparisonType { get; set; } = RemodelComparisonType.Any;
}
