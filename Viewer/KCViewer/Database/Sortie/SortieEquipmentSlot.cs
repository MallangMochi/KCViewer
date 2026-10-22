using System.Text.Json.Serialization;

namespace KCViewer.Database.Sortie;

public class SortieEquipmentSlot
{
	[JsonPropertyName("AircraftCurrent")]
	public int AircraftCurrent { get; set; }

	[JsonPropertyName("AircraftMax")]
	public int AircraftMax { get; set; }

	[JsonPropertyName("Equipment")]
	public SortieEquipment? Equipment { get; set; }
}
