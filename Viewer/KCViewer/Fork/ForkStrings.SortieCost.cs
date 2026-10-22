using KCViewer.Translations;

namespace KCViewer.Fork;

/// <summary>
/// Fork: 출격 코스트 in KanColleViewer's style (KcvSortieCostWindow, mockup 20261017-sortie-cost-v3). The names the user
/// gave in Korean (2026-10-17); in another language the viewer's own words.
/// </summary>
public static partial class ForkStrings
{
	private static string KoreanOr(string korean, string viewer) => IsKorean ? korean : viewer;

	/// <summary>The bucket choice's damage below 소파 (the viewer's 「통상 피해」).</summary>
	public static string SortieCostScratch => KoreanOr("피해", SortieCostViewerResources.NormalDamage);

	public static string SortieCostBucketNote => T(
		"이번 출격에서 새로 그 피해를 입은 함선마다 1개",
		"今回の出撃で新たにその損傷を受けた艦ごとに1個",
		"One for each ship newly damaged that far in the sortie");

	/// <summary>The sortie fleet's jet cost (the viewer's 「제트」).</summary>
	public static string SortieCostJet => KoreanOr("분식보급", SortieCostViewerResources.SortieJet);

	public static string SortieCostAirBaseJet => KoreanOr("기지 항공대 분식보급", SortieCostViewerResources.AirBaseJet);

	/// <summary>The resources picked up on resource nodes (the viewer's 「아이템 획득」).</summary>
	public static string SortieCostResourceNodes => KoreanOr("자원 칸", SortieCostViewerResources.ResourceNodes);

	/// <summary>The fuel and ammo of the fleet's sunk ships (the viewer's 「격침」).</summary>
	public static string SortieCostSinking => KoreanOr("아군 격침", SortieCostViewerResources.Sinking);

	public static string SortieCostTotal(int count) => T(
		$"총 출격 코스트 ({count}건)",
		$"総出撃コスト（{count}件）",
		$"Total sortie cost ({count})");

	public static string SortieCostCalculating(int done, int total) => T(
		$"계산 중… {done} / {total}",
		$"計算中… {done} / {total}",
		$"Calculating… {done} / {total}");
}
