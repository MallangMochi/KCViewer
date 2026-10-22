using System.Text.Json.Serialization;

namespace KCViewer.Data.PoiDbSubmission.PoiDbBattleSubmission;

public class PoiDbBattleSubmissionData
{
	[JsonPropertyName("data")]
	public required Data Data { get; init; }
}
