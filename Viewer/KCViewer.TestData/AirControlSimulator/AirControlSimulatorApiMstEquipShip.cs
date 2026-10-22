using System.Text.Json.Serialization;
using KCViewer.Core.Types;

namespace KCViewer.TestData.AirControlSimulator;

public class AirControlSimulatorApiMstEquipShip
{
	/// <summary>
	/// Key is <see cref="EquipmentTypes"/>.
	/// </summary>
	[JsonPropertyName("api_equip_type")]
	public Dictionary<string, List<EquipmentId>?> ApiEquipType { get; set; } = [];
}
