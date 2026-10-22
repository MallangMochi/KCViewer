using System.Text.Json.Serialization;

namespace KCViewer.Core.Types.Serialization.EquipmentUpgrade;

public class EquipmentUpgradeConversionModel
{
	[JsonPropertyName("id_after")]
	public int IdEquipmentAfter { get; set; }

	[JsonPropertyName("lvl_after")]
	public int EquipmentLevelAfter { get; set; }
}
