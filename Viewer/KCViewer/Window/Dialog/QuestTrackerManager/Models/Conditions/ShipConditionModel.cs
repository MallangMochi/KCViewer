using System;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core.Types;
using KCViewer.Window.Dialog.QuestTrackerManager.Enums;
using MessagePack;

namespace KCViewer.Window.Dialog.QuestTrackerManager.Models.Conditions;

[Obsolete("Use ShipConditionModelV2")]
[MessagePackObject]
public class ShipConditionModel : ObservableObject, ICondition
{
	[Key(0)] public ShipId Id { get; set; } = ShipId.Kamikaze;
	[Key(1)] public bool MustBeFlagship { get; set; }
	[Key(2)] public RemodelComparisonType RemodelComparisonType { get; set; } = RemodelComparisonType.Any;
}
