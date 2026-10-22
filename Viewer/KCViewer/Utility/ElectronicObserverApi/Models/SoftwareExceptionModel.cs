using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KCViewer.Utility.ElectronicObserverApi.Models;

public record SoftwareExceptionModel
{
	[JsonPropertyName("type")] public string Type { get; set; } = "";

	[JsonPropertyName("message")] public string Message { get; set; } = "";

	[JsonPropertyName("stackTrace")] public string StackTrace { get; set; } = "";

	[JsonPropertyName("innerExceptions")] public List<SoftwareExceptionModel> InnerExceptions { get; set; } = [];
}
