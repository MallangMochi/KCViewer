using System.Text.Json.Serialization;

namespace KCViewer.TestData.AirControlSimulator;

public class AirControlSimulatorWorld
{
	[JsonPropertyName("world")]
	public int World { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;
}
