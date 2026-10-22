using System.Text.Json.Serialization;

namespace KCViewer.Utility.ElectronicObserverApi.Models;

public class SoftwareIssueModel
{
	[JsonPropertyName("softwareVersion")] public string SoftwareVersion { get; set; } = "";

	[JsonPropertyName("exception")] public required SoftwareExceptionModel Exception { get; set; }
}
