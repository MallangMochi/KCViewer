using System.Text.Json.Serialization;

namespace KCViewer.Core.Types.Serialization.DeckBuilder;

public class DeckBuilderSpecialEffectItem
{
	[JsonPropertyName("kind")] public SpEffectItemKind SpEffectItemKind { get; set; }
	[JsonPropertyName("fp")] public int Firepower { get; set; }
	[JsonPropertyName("tp")] public int Torpedo { get; set; }
	[JsonPropertyName("ar")] public int Armor { get; set; }
	[JsonPropertyName("ev")] public int Evasion { get; set; }
}
