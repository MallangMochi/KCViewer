using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.Interfaces;

public interface INightFriendFleetApiResponse : IFriendFleetApiResponse
{
	/// <summary>
	/// 友軍艦隊攻撃　発動時のみ存在？
	/// </summary>
	ApiFriendlyBattle? ApiFriendlyBattle { get; set; }
}
