using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.Interfaces;

public interface IFriendFleetApiResponse
{
	/// <summary>
	/// 友軍艦隊攻撃情報　発動時のみ存在
	/// </summary>
	ApiFriendlyInfo? ApiFriendlyInfo { get; set; }
}
