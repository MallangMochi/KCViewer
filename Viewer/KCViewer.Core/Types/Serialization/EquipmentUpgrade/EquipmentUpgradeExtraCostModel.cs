using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KCViewer.Core.Types.Serialization.EquipmentUpgrade;

public class EquipmentUpgradeExtraCostModel
{
	[JsonPropertyName("levels")]
	public List<UpgradeLevel> Levels { get; set; } = [];

	[JsonPropertyName("consumable")]
	public List<EquipmentUpgradeImprovementCostItemDetail> Consumables { get; set; } = [];

	[JsonPropertyName("equips")]
	public List<EquipmentUpgradeImprovementCostItemDetail> Equipments { get; set; } = [];
}
