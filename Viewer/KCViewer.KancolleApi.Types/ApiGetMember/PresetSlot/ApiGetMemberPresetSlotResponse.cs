using KCViewer.KancolleApi.Types.ApiGetMember.Models;

namespace KCViewer.KancolleApi.Types.ApiGetMember.PresetSlot;

public class ApiGetMemberPresetSlotResponse
{
	[JsonPropertyName("api_max_num")]
	public int ApiMaxNum { get; set; }

	[JsonPropertyName("api_preset_items")]
	public List<ApiPresetItem> ApiPresetItems { get; set; } = new();
}
