using System.Collections.Generic;
using System.Text.Json.Serialization;
using KCViewer.Core.Types;

namespace KCViewer.Database.Sortie;

public class SortieAirBase
{
	[JsonPropertyName("Name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("MapAreaId")]
	public int MapAreaId { get; set; }

	[JsonPropertyName("AirCorpsId")]
	public int AirCorpsId { get; set; }

	[JsonPropertyName("ActionKind")]
	public AirBaseActionKind ActionKind { get; set; }

	[JsonPropertyName("BaseDistance")]
	public int BaseDistance { get; set; }

	[JsonPropertyName("BonusDistance")]
	public int BonusDistance { get; set; }

	[JsonPropertyName("Squadrons")]
	public List<SortieAirBaseSquadron> Squadrons { get; set; } = new();
}
