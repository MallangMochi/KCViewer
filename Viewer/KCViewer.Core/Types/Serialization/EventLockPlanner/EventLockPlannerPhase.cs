using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KCViewer.Core.Types.Serialization.EventLockPlanner;

public class EventLockPlannerPhase
{
	[JsonPropertyName("LockGroups")] public List<int> LockGroups { get; set; } = new();
	[JsonPropertyName("Name")] public string Name { get; set; } = "";
}
