using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiPort.Port;

public class ApiPortAirCorpsCondRecoveryWithTimerResponse
{
	[JsonPropertyName("api_plane_info")]
	public ApiPortPlaneInfo? ApiPlaneInfo { get; set; }

	[JsonPropertyName("api_distance")]
	public ApiDistance? ApiDistance { get; set; } = new();
}
