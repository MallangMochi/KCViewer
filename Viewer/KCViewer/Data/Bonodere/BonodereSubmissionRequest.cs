using System.Collections.Generic;
using System.Text.Json.Serialization;
using KCViewer.Window.Wpf.SenkaLeaderboard;

namespace KCViewer.Data.Bonodere;

public class BonodereSubmissionRequest
{
	[JsonPropertyName("data")]
	public required List<SenkaEntryModel> Data { get; set; }
}
