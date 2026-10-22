using System.Text.Json.Serialization;

namespace KCViewer.Avalonia.Translation.Mission;

public class MissionData
{
	[JsonPropertyName("name_jp")]
	public required string NameJp { get; init; }
	
	[JsonPropertyName("name")]
	public required string Name { get; init; }
	
	[JsonPropertyName("desc_jp")]
	public string? DescriptionJp { get; init; }
	
	[JsonPropertyName("desc")]
	public string? Description { get; init; }
}
