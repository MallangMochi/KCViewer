using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core.Types;
using MessagePack;

namespace KCViewer.Window.Dialog.QuestTrackerManager.Models.Conditions;

[MessagePackObject]
public class AllowedShipTypesConditionModel : ObservableObject, ICondition
{
	[Key(0)] public ObservableCollection<ShipTypes> Types { get; set; } = new() { ShipTypes.Destroyer };
}
