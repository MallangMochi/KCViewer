using System.Text.Json.Serialization;

namespace KCViewer.Avalonia.Translation.Quest;

/// <summary>
/// Fork: a quest's rewards besides the four resources (the game sends those with the quest), from the viewer's own
/// translation data (tools/kcv-data: KC3's quest memos, KC3Kai's consumables), user's decision 2026-10-07 (mockup
/// 20261007-quest-rewards-v1).
/// </summary>
public sealed class QuestReward
{
	/// <summary>What is received.</summary>
	[JsonPropertyName("get")]
	public List<QuestRewardItem> Get { get; init; } = [];

	/// <summary>The choice groups, one pick from each.</summary>
	[JsonPropertyName("choices")]
	public List<List<QuestRewardItem>> Choices { get; init; } = [];
}

public sealed class QuestRewardItem
{
	/// <summary>Korean.</summary>
	[JsonPropertyName("name")]
	public string Name { get; init; } = "";

	/// <summary>As the memo writes it (a ship with its type: 駆逐艦白雪).</summary>
	[JsonPropertyName("jp")]
	public string NameJp { get; init; } = "";

	[JsonPropertyName("count")]
	public int Count { get; init; } = 1;

	/// <summary>The improvement level of an equipment, 10 for max.</summary>
	[JsonPropertyName("stars")]
	public int Stars { get; init; }

	/// <summary>ship, equipment, item, furniture, feature (a slot or an unlock), other.</summary>
	[JsonPropertyName("kind")]
	public string Kind { get; init; } = "";

	/// <summary>A ship's type (구축함).</summary>
	[JsonPropertyName("type")]
	public string? Type { get; init; }
}
