using System.Text.Json.Serialization;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;

namespace KCViewer.Window.Settings.SubWindow.Fleet;

public record GaugeConfiguration
{
	[JsonIgnore]
	public string Name => $"{TpGauge.GetEventName()} E{TpGauge.GetGaugeMapId()}-{TpGauge.GetGaugeIndex()}";

	public required TpGauge TpGauge { get; set; }

	public bool ShouldDisplay { get; set; } = true;
}
