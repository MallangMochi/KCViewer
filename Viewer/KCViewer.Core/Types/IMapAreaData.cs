using System.Collections.Generic;
using KCViewer.Core.Types.Data;

namespace KCViewer.Core.Types;

public interface IMapAreaData : IIdentifiable, IApiWrapper
{
	/// <summary>
	/// 海域カテゴリID
	/// </summary>
	public int MapAreaID { get; }

	/// <summary>
	/// 海域カテゴリ名
	/// </summary>
	public string Name { get; }

	public string NameEN { get; }

	/// <summary>
	/// 海域タイプ　0=通常, 1=イベント
	/// </summary>
	public int MapType { get; }

	public bool IsEventArea { get; }
}
