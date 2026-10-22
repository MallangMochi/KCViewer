namespace KCViewer.Fork;

/// <summary>
/// Fork: the ship group's CSV output in KanColleViewer's style (KcvShipGroupCsvWindow, mockup 20261019-group-csv-v2,
/// approved 2026-10-19 "구현"). The window's labels are the viewer's.
/// </summary>
public static partial class ForkStrings
{
	/// <summary>
	/// 열람 용's header in Korean on a Korean screen (the user's answer 2026-10-19 "2.나"): the ship group table's column
	/// names, 현재 HP · 최대 HP from the filter's items. Null on other screens, which keep the viewer's Japanese; 데이터 용
	/// keeps it on every screen.
	/// </summary>
	public static string? ShipCsvUserHeader => IsKorean
		? "ID,함종,함명,Lv,Exp,next,개장까지,현재 HP,최대 HP,Cond,연료,탄약,장비1,장비2,장비3,장비4,장비5,보강,입거 시간,"
			+ "화력,화력개수,화력합계,뇌장,뇌장개수,뇌장합계,대공,대공개수,대공합계,장갑,장갑개수,장갑합계,대잠,대잠합계,회피,회피합계,"
			+ "색적,색적합계,운,운개수,운합계,사거리,속력,잠금,출격처,정렬 ID,항공위력,포격위력,공격위력,대잠위력,뇌장위력,야전 위력"
		: null;
}
