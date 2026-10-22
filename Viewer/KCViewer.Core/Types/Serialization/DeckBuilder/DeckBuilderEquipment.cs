using System.Text.Json.Serialization;

namespace KCViewer.Core.Types.Serialization.DeckBuilder;

public class DeckBuilderEquipment
{
	[JsonPropertyName("id")] public EquipmentId Id { get; set; }
	[JsonPropertyName("rf")] public int Level { get; set; }
	[JsonPropertyName("mas")] public int? AircraftLevel { get; set; }
	[JsonPropertyName("ac")] public int? AircraftCount { get; set; }
}
