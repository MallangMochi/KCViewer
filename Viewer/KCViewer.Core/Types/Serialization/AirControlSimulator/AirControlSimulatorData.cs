using System.Collections.Generic;
using System.Text.Json.Serialization;
using KCViewer.Core.Types.Serialization.DeckBuilder;
using KCViewer.Core.Types.Serialization.FleetAnalysis;

namespace KCViewer.Core.Types.Serialization.AirControlSimulator;

public class AirControlSimulatorData
{
	[JsonPropertyName("predeck")] public DeckBuilderData? Fleet { get; set; }
	[JsonPropertyName("ships")] public IEnumerable<FleetAnalysisShip>? Ships { get; set; }
	[JsonPropertyName("items")] public IEnumerable<FleetAnalysisEquipment>? Equipment { get; set; }
}
