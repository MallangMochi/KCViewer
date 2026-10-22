using System.Text.Json.Serialization;
using KCViewer.Core.Types;

namespace KCViewer.Database.Sortie;

public class SortieAirBaseSquadron
{
	[JsonPropertyName("AircraftCurrent")]
	public int? AircraftCurrent { get; set; }

	[JsonPropertyName("State")]
	public int State { get; set; }

	[JsonPropertyName("Condition")]
	public AirBaseCondition Condition { get; set; }

	[JsonPropertyName("EquipmentSlot")]
	public SortieEquipmentSlot EquipmentSlot { get; set; } = new();
}
