namespace KCViewer.Fork;

/// <summary>
/// Fork: Korean for text that the upstream code has as literals instead of resources, where it is often seen
/// (VIEWER-PLAN.md, stage 6 (3), user decision: the frequently seen places only). Any other UI language keeps the
/// original literal, Japanese or English, so only the Korean UI changes.
/// </summary>
public static partial class ForkStrings
{
	private static string K(string korean, string original) => IsKorean ? korean : original;

	/// <summary>
	/// The viewer's own name for the Korean UI, in place of SoftwareInformation.SoftwareNameJapanese (七四式電子観測儀)
	/// (user's decision 2026-10-22 on going independent of 74식: 「시제 74식 제독업무도 바빠 改」).
	/// </summary>
	public static string SoftwareNameKorean => "시제 74식 제독업무도 바빠 改";

	/// <summary>
	/// The viewer's name in the current UI language: <see cref="SoftwareNameKorean"/> for the Korean UI, else KCViewer
	/// ("일본어 영어는 KCViewer 로 일단 띄워 놔", 2026-10-22), as the window titles (MainResources.Title, Menus.Title) have it:
	/// the version windows, the start log, the closing question, the first-start message, the second-instance message.
	/// </summary>
	public static string ProductDisplayName => K(SoftwareNameKorean, ForkVersion.ProductName);

	// Battle panel tooltips and the battle detail window
	public static string BattleMiss => K("빗나감", "Miss");
	public static string BattleDamageSuffix => K(" 피해", " Dmg");
	public static string BattleCriticalSuffix => K(" 치명타!", " Critical!");
	public static string None => K("(없음)", "(なし)");

	// Phase titles missing from PhaseBase's translation table
	public static string PhaseFriendlyFleet => K("우군 함대", "友軍艦隊");
	public static string PhaseFriendlySupportAirAttack => K("우군 지원 항공 공격", "友軍支援航空攻撃");
	public static string PhaseFriendlyFleetCover => K("우군 함대 엄호", "友軍艦隊援護");
	public static string PhaseNightBattleStart => K("야전 개시", "夜戦開始");
	public static string PhaseRadarShelling => K("레이더 사격", "レーダー射撃");

	/// <summary>Between the alternatives of an anti-air cut-in's conditions.</summary>
	public static string Or => K("또는", "OR");

	// Fleet panel: the level tooltip, and the compass panel
	public static string ExpTotalLine => K("합계: {0:N0} exp.\r\n", "Total: {0:N0} exp.\r\n");
	public static string CompassExp => K("경험치: ", "Exp: ");

	// Notifications
	public static string AirCorpsSquadron => K("중대", "squad");
	public static string BattleNotificationTitle => K("전투 알람", "Battle notification");
	public static string BattleIdleWarning => K("전투 중에 오래 멈춰 있습니다.", "Idle warning.");
	public static string BattleEnded => K("전투가 끝났습니다.", "Battle ended.");
	public static string ConstructedShip => K("함선", "艦娘");

	// Expedition check: failure reasons
	public static string MissionEscortFleetDD4 => K("호위대(경순1구축4외)", "護衛隊(軽巡1駆逐4他)");
	public static string MissionConditionOr => K(") 또는 (", ") or (");

	// BGM settings: the scenes without a resource
	public static string BgmPracticeDay => K("연습 주간전", "演習昼戦");
	public static string BgmPracticeNight => K("연습 야전", "演習夜戦");
	public static string BgmResultWin => K("승리", "勝利");
	public static string BgmResultLose => K("패배", "敗北");
	public static string BgmResultBossWin => K("보스 승리", "ボス勝利");
	public static string BgmRecord => K("전적", "戦績");
	public static string BgmItem => K("아이템", "アイテム");
	public static string BgmQuest => K("임무", "任務");
	public static string BgmAlbum => K("도감", "図鑑");
	public static string BgmImprovementArsenal => K("개수 공창", "改修工廠");

	/// <summary>Sound file dialog of the notification and BGM settings.</summary>
	public static string MusicFiles => K("음악 파일", "音楽ファイル");
}
