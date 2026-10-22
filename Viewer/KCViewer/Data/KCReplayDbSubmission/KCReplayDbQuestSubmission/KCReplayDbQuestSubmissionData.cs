using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.Data.KCReplayDbSubmission.KCReplayDbQuestSubmission;

public class KCReplayDbQuestSubmissionData
{
	/// <summary>
	/// Should be <see cref="ApiListClass" /> but we're skipping deserialization to avoid data loss.
	/// </summary>
	[JsonPropertyName("list")]
	public required List<JsonNode> QuestsData { get; init; }
}
