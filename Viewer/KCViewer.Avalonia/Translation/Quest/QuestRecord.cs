using System.Text.Json.Serialization;

namespace KCViewer.Avalonia.Translation.Quest;

public class QuestRecord
{
	[JsonPropertyName("code")]
	public required string Code { get; init; }

	[JsonPropertyName("name_jp")]
	public required string NameJp { get; init; }

	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[JsonPropertyName("desc_jp")]
	public required string DescriptionJp { get; init; }

	[JsonPropertyName("desc")]
	public required string Description { get; init; }

	// Fork: the rewards of the viewer's own translation data (absent from EO's)
	[JsonPropertyName("reward")]
	public QuestReward? Reward { get; init; }
}
