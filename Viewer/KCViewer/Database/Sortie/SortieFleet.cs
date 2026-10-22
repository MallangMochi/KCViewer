using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KCViewer.Database.Sortie;

public class SortieFleet
{
	[JsonPropertyName("Name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("Ships")]
	public List<SortieShip> Ships { get; set; } = new();
}
