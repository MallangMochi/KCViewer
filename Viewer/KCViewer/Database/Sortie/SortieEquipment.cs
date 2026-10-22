using System.Text.Json.Serialization;
using KCViewer.Core.Types;

namespace KCViewer.Database.Sortie;

public class SortieEquipment
{
	[JsonPropertyName("Id")]
	public EquipmentId Id { get; set; }

	[JsonPropertyName("Level")]
	public int Level { get; set; }

	[JsonPropertyName("AircraftLevel")]
	public int AircraftLevel { get; set; }
}
