using System.Collections.Generic;
using System.Globalization;

namespace KCViewer.Fork;

/// <summary>
/// Fork: UI text of the fork's own parts. Korean when the UI culture is Korean, English otherwise.
/// Plugins carry their own text.
/// </summary>
public static partial class ForkStrings
{
	private static bool IsKorean => CultureInfo.CurrentUICulture.Name.StartsWith("ko", System.StringComparison.Ordinal);

	private static string T(string korean, string english) => IsKorean ? korean : english;

	public static string Test => T("테스트", "Test");

	// the top menus split as decided (VIEWER-PLAN "상단 메뉴 개편", mockup 5 and the menu check of 2026-10-05)
	public static string PanelsMenu => T("패널(_P)", "パネル(_P)", "_Panels");
	public static string SkinMenu => T("스킨(_S)", "スキン(_S)", "_Skin");
	public static string PluginsMenu => T("플러그인(_L)", "プラグイン(_L)", "P_lugins");
	public static string OpenPluginFolder => T("플러그인 폴더 열기(_O)", "プラグインフォルダーを開く(_O)", "_Open plugin folder");

	// the help menu's items (its update check, the links) are in ForkStrings.ProgramUpdateUi.cs

	// the version window (Fork/ForkVersion.cs)
	public static string VersionCommit(string? release, string? commit, string? date) =>
		T("버전: ", "バージョン: ", "Version: ") + VersionValue(release, commit, date);

	/// <summary>A release "1.0.0 (989cf4909, 2026-10-17 커밋)", a development build "989cf4909 (2026-10-17 커밋)".</summary>
	public static string VersionValue(string? release, string? commit, string? date) => (release, commit) switch
	{
		(null, null) => T("알 수 없음(git 없이 빌드)", "不明(git なしでビルド)", "unknown (built without git)"),
		(null, _) => T($"{commit} ({date} 커밋)", $"{commit} ({date} のコミット)", $"{commit} (committed {date})"),
		(_, null) => release,
		_ => T($"{release} ({commit}, {date} 커밋)", $"{release} ({commit}, {date} のコミット)", $"{release} ({commit}, committed {date})"),
	};

	public static string VersionBuilt(string time) => T($"빌드: {time}", $"ビルド: {time}", $"Built: {time}");
	public static string VersionBase(string name, string version) => T("기반: ", "ベース: ", "Based on ") + VersionBaseValue(name, version);
	public static string VersionBaseValue(string name, string version) => T($"74식 {name} {version}", $"{name} {version}", $"{name} {version}");
	public static string VersionOriginalDeveloper => T("74식 개발: ", "七四式 開発: ", "Electronic Observer by: ");
	public static string VersionOriginalMaintainers => T("74식 수정: ", "七四式 改修: ", "Modified by: ");

	/// <summary>"라이트" → "라이트(_L)" on the Korean screen, where the menus have the keys the user chose; as it is elsewhere.</summary>
	public static string WithKey(string text, char key) => IsKorean ? $"{text}(_{key})" : text;

	public static string LocalSkin => T("로컬 스킨", "Local skin");
	public static string LocalSkinEnabled => T("로컬 스킨 사용", "Use local skin");
	public static string LocalSkinReload => T("스킨 파일 다시 읽기", "Reload skin files");
	public static string LocalSkinOpenFolder => T("스킨 폴더 열기", "Open skin folder");

	public static string KcvStyle => T("제독업무 스타일", "KanColleViewer style");
	public static string KcvStyleEnabled => T("제독업무 스타일 사용", "Use KanColleViewer style");
	public static string ThemeLight => T("라이트", "Light");
	public static string ThemeDark => T("다크", "Dark");
	public static string AccentPurple => T("강조색: 보라", "Accent: purple");
	public static string AccentBlue => T("강조색: 파랑", "Accent: blue");
	public static string AccentOrange => T("강조색: 주황", "Accent: orange");

	public static string LocalSkinTurnedOn(string folder) => T(
		$"로컬 스킨을 켰습니다({folder}). 게임을 새로 고치면 적용됩니다.",
		$"Local skin on ({folder}). Reload the game to apply it.");

	public static string LocalSkinTurnedOff => T(
		"로컬 스킨을 껐습니다. 게임을 새로 고치면 원래 파일로 돌아갑니다.",
		"Local skin off. Reload the game to get the original files back.");

	public static string SoftwareUpdateDisabled => T(
		"포크 빌드는 원본 74식의 프로그램 업데이트를 받지 않습니다. 원본 변경은 git으로 합칩니다.",
		"This fork build does not install upstream program updates. Upstream changes are merged with git.");

	// Labels of the KanColleViewer panel designs (Fork/KcvViews). Japanese as KanColleViewer shows them, for a
	// Japanese UI; Korean or English otherwise.
	private static bool IsJapanese => CultureInfo.CurrentUICulture.Name.StartsWith("ja", System.StringComparison.Ordinal);

	private static string T(string korean, string japanese, string english) =>
		IsKorean ? korean : IsJapanese ? japanese : english;

	public static string KcvHqLevel => T("사령부 Lv.", "司令部 Lv.", "HQ Lv.");
	public static string KcvDock => T("입거", "入渠", "Docks");
	public static string KcvArsenal => T("건조", "建造", "Construction");
	public static string KcvDockShip => T("입거 중인 함선", "入渠中の艦娘", "Ship in dock");
	public static string KcvArsenalShip => T("건조 중인 함선", "建造中の艦娘", "Ship being built");
	public static string KcvRemainingTime => T("남은 시간", "残り時間", "Remaining");
	public static string KcvEmpty => T("미사용", "未使用", "Empty");
	// a dock or construction dock not opened yet (as 함대 미개방 of the expedition panel, user's choice 2026-10-02)
	public static string KcvLocked => T("도크 미개방", "ドック未開放", "Dock locked");
	public static string KcvQuests => T("임무", "任務", "Quests");
	// The quest panel's header count and category filters (mockup 20261005-quest-filter-v2)
	public static string KcvQuestsInProgressCount(int running, int max) => T($"수행 중인 임무 ({running}/{max})", $"現在進行中の任務 ({running}/{max})", $"Quests in progress ({running}/{max})");
	public static string KcvQuestsInProgressCount(int running) => T($"수행 중인 임무 ({running})", $"現在進行中の任務 ({running})", $"Quests in progress ({running})");
	public static string KcvQuestFilterComposition => T("편성", "編成", "Composition");
	public static string KcvQuestFilterBattle => T("전투", "戦闘", "Battle");
	public static string KcvQuestFilterBattleToolTip => T("출격 임무", "出撃任務", "Sortie quests");
	public static string KcvQuestFilterPractice => T("연습", "演習", "Exercise");
	public static string KcvQuestFilterExpedition => T("원정", "遠征", "Expedition");
	public static string KcvQuestFilterArsenal => T("공창", "工廠", "Arsenal");
	public static string KcvQuestFilterOther => T("그외", "その他", "Other");
	public static string KcvQuestFilterOtherToolTip => T("그외: 보급·입거, 개장, 그 밖", "その他: 補給・入渠、改装ほか", "Other: supply and docking, modernization and the rest");
	public static string KcvShowInProgressOnly => T("수행 중만 표시", "遂行中のみ表示", "In progress only");
	public static string KcvShowAllQuests => T("모든 임무 표시", "全任務を表示", "Show all quests");
	public static string KcvQuestInProgress => T("수행 중", "遂行中", "In progress");
	// The quest panel's rewards (mockup 20261007-quest-rewards-v1): a line under the description and the name's tooltip
	public static string KcvQuestReward => T("보상", "報酬", "Rewards");
	// how many choice groups, in the line under the description (user's decision 2026-10-07: 「선택 보상 2개」)
	public static string KcvQuestRewardChoices(int count) => T($"선택 보상 {count}개", $"選択報酬 {count}件", $"{count} choice rewards");
	// the tooltip's headings, without 「(하나 고름)」 (user's decisions 2026-10-07 "괄호는 없애버려줄래", "선택 문구는 선택 보상
	// 으로 해줘 선택 보상 1")
	public static string KcvQuestRewardChoice => T("선택 보상", "選択報酬", "Choice reward");
	public static string KcvQuestRewardChoiceN(int n) => T($"선택 보상 {n}", $"選択報酬 {n}", $"Choice reward {n}");
	public static string[] KcvQuestRewardResources => [KcvChartFuel, KcvChartAmmo, KcvChartSteel, KcvChartBauxite];
	// the line under the description is short: 보크
	public static string[] KcvQuestRewardResourcesShort => [KcvChartFuel, KcvChartAmmo, KcvChartSteel, T("보크", "ボーキ", "Baux")];
	// The ship training plan editor (mockup 20261008-ship-training-edit-v2): the titles (user's decision 2026-10-08 "나눔"),
	// the goal table, the limits and marks, the remodel box
	public static string KcvTrainingPlanAdd => T("계획 추가", "計画の追加", "Add plan");
	public static string KcvTrainingPlanEdit => T("계획 편집", "計画の編集", "Edit plan");
	public static string KcvTrainingGoal => T("목표", "目標", "Goal");
	public static string KcvTrainingCurrent => T("현재", "現在", "Now");
	public static string KcvTrainingLeft => T("남음", "残り", "Left");
	public static string KcvTrainingLevel => T("레벨", "レベル", "Level");
	public static string KcvTrainingModernize => T("개수", "改修", "Mod");
	public static string KcvTrainingMax(string value) => T($"최대 {value}", $"最大 {value}", $"max {value}");
	public static string KcvTrainingNoModernize => T("개수 불가", "改修不可", "No mod");
	public static string KcvTrainingDone => T("달성", "達成", "Done");
	public static string KcvTrainingExpLeft(int exp) => T($"경험치 {exp:N0}", $"経験値 {exp:N0}", $"{exp:N0} exp");
	public static string KcvTrainingRemodel => T("개조", "改装", "Remodel");
	public static string KcvTrainingRemodelGoal => T("개조 목표", "改装目標", "Remodel goal");
	public static string KcvTrainingRemodelReady => T("개조 가능", "改装可能", "Can remodel");
	public static string KcvTrainingLastRemodel => T("마지막 개조", "最終改装", "Final remodel");
	public static string KcvTrainingNotifyAny => T("개조할 수 있을 때마다 알람", "改装できるたびに通知", "Alarm at every remodel");
	// 개수 계획 관리's sub-windows (mockup 20261009-upgrade-plan-windows-v2): the plan editor, the upgrade tree
	public static string KcvUpgradePlanAdd => T("계획 추가", "計画の追加", "Add plan");
	public static string KcvUpgradePlanEdit => T("계획 편집", "計画の編集", "Edit plan");
	public static string KcvUpgradePlanBox => T("계획", "計画", "Plan");
	public static string KcvUpgradeChangeEquipment => T("장비 바꾸기", "装備の変更", "Change equipment");
	public static string KcvUpgradeTreeTitle(string equipment) => T($"개수 트리 - {equipment}", $"改修ツリー - {equipment}", $"Upgrade tree - {equipment}");
	public static string KcvExpandAll => T("모두 펼치기", "すべて展開", "Expand all");
	public static string KcvCollapseAll => T("모두 접기", "すべて折りたたむ", "Collapse all");
	public static string KcvUpgradeTreeHint => T("항목을 우클릭하면 계획 추가·편집·삭제와 장비 할당", "右クリックで計画の追加・編集・削除と装備の割り当て", "Right-click an item to add, edit or remove a plan or assign equipment");
	public static string KcvUpgradeTreeItem => T("항목", "項目", "Item");
	public static string KcvUpgradeTreeGoal => T("목표", "目標", "Goal");
	public static string KcvUpgradeTreeCraft => T("개발·입수", "開発・入手", "Develop / get");
	public static string KcvUpgradeTreeOwned => T("할당됨", "割り当て済み", "Assigned");
	public static string KcvUpgradeTreeStatus(int plans, int crafts) => T(
		$"계획 {plans}개 · 개발·입수 {crafts}묶음 (펼친 것 기준)",
		$"計画 {plans}件 · 開発・入手 {crafts}件 (展開中のみ)",
		$"{plans} plans · {crafts} to develop or get (expanded items)");
	public static string KcvTrainingNotifyAnyToolTip => T(
		"예: 카미카제(Lv.1)를 카미카제 改2까지 키울 때\n끄면: 카미카제 改2로 개조할 수 있을 때만 알람을 띄웁니다.\n켜면: 카미카제 改로 개조할 수 있을 때와 카미카제 改2로 개조할 수 있을 때 모두 알람을 띄웁니다.",
		"例: 神風(Lv.1)を神風改二まで育てる場合\nオフ: 神風改二に改装できるときだけ通知します。\nオン: 神風改に改装できるときと、神風改二に改装できるときの両方で通知します。",
		"Example: training Kamikaze (Lv.1) up to Kamikaze Kai Ni\nOff: an alarm only when she can be remodelled to Kai Ni.\nOn: an alarm when she can be remodelled to Kai and again when she can be remodelled to Kai Ni.");
	public static string KcvRefresh => T("표시 갱신", "表示の更新", "Refresh");
	public static string KcvEquipmentName => T("장비명", "装備名", "Equipment");
	public static string KcvTotal => T("합계", "合計", "Total");
	public static string KcvImprovement => T("개수", "改修", "Level");
	public static string KcvOwnedSpare => T("보유 수 (남음)", "所持数 (余り)", "Owned (spare)");
	public static string KcvEquippedBy => T("장착한 함선", "装備している艦", "Equipped by");
	public static string KcvRelocating => T("배치 전환 중", "配置転換中", "Relocating");
	public static string KcvToggleAll => T("전체 선택·해제", "すべて選択・解除", "All on / off");
	public static string KcvAppliedCategories(int shown, int all) => T($"분류 {shown}/{all}", $"分類 {shown}/{all}", $"Categories {shown}/{all}");

	// 所属艦娘一覧 (KcvShipListWindow)
	public static string KcvShipList => T("함선 목록", "所属艦娘一覧", "Ship list");
	public static string KcvFilterSettings => T("필터 설정", "フィルター設定", "Filters");
	/// <summary>Beside the name box while the filters are folded (also the equipment list): the filters not at their default.</summary>
	public static string KcvFiltersApplied(IEnumerable<string> filters) => T($"적용 중: {string.Join(", ", filters)}", $"適用中: {string.Join("、", filters)}", $"Applied: {string.Join(", ", filters)}");
	public static string KcvShipType => T("함종:", "艦種:", "Type:");
	public static string KcvSelectAll => T("전체 선택", "すべて選択", "Select all");
	public static string KcvPresetDestroyers => T("구축함", "駆逐艦", "Destroyers");
	public static string KcvPresetLightCruisers => T("경순·뇌순", "軽巡・雷巡", "CL・CLT");
	public static string KcvPresetHeavyCruisers => T("중순·항순", "重巡・航巡", "CA・CAV");
	public static string KcvPresetBattleships => T("전함", "戦艦", "Battleships");
	public static string KcvPresetCarriers => T("항공모함", "航空母艦", "Carriers");
	public static string KcvPresetSubmarines => T("잠수함", "潜水艦", "Submarines");
	public static string KcvPresetAviation => T("항전·항순", "航戦・航巡", "BBV・CAV");
	public static string KcvAll => T("전체", "すべて", "All");
	public static string KcvLevel => T("레벨:", "レベル:", "Level:");
	public static string KcvLevel2Plus => T("Lv.2 이상", "Lv.2 以上", "Lv.2+");
	public static string KcvLock => T("잠금:", "ロック:", "Lock:");
	public static string KcvLockedShips => T("잠금", "ロックしている", "Locked");
	public static string KcvUnlockedShips => T("잠금 안 함", "ロックしていない", "Unlocked");
	// the reinforcement expansion slot (mockup 20261024-shiplist-exslot-v2, approved "구현")
	public static string KcvExpansionSlot => T("보강 증설:", "補強増設:", "Expansion slot:");
	public static string KcvExpansionSlotOpen => T("개방", "開放", "Open");
	public static string KcvExpansionSlotClosed => T("미개방", "未開放", "Not open");
	public static string KcvExpedition => T("원정:", "遠征:", "Expedition:");
	public static string KcvExcludeExpedition => T("원정 중인 함선 제외", "遠征中の艦を除外する", "Exclude ships on expedition");
	public static string KcvSpeed => T("속력:", "速力:", "Speed:");
	public static string KcvFast => T("고속", "高速", "Fast");
	public static string KcvSlow => T("저속", "低速", "Slow");
	public static string KcvRemodel => T("개장:", "改造:", "Remodel:");
	public static string KcvRemodelDone => T("개장 완료", "改造完了", "Done");
	public static string KcvRemodelNotDone => T("개장 미완", "改造未完", "Not done");
	public static string KcvModernization => T("근대화 개수:", "近代化改修:", "Modernization:");
	public static string KcvModernizationDone => T("개수 완료", "改修完了", "Done");
	public static string KcvModernizationNotDone => T("개수 미완", "改修未完", "Not done");
	public static string KcvColumnType => T("함종", "艦種", "Type");
	public static string KcvColumnName => T("함선명", "艦名", "Name");
	public static string KcvColumnLevel => T("레벨", "レベル", "Level");
	public static string KcvColumnFirepower => T("화력", "火力", "FP");
	public static string KcvColumnTorpedo => T("뇌장", "雷装", "TP");
	public static string KcvColumnAA => T("대공", "対空", "AA");
	public static string KcvColumnArmor => T("장갑", "装甲", "AR");
	public static string KcvColumnLuck => T("운", "運", "Luck");
	public static string KcvColumnLos => T("색적", "索敵", "LoS");
	public static string KcvColumnEquipment => T("장비", "装備", "Equipment");

	// 艦隊詳細 (KcvFleetDetailWindow)
	public static string KcvFleetDetail => T("함대 상세", "艦隊詳細", "Fleet details");

	// the combined fleet panel and the fleet detail's combined fleet (mockups 20261009-combined-fleet-panel-v3, -detail-v1)
	public static string KcvCombinedFleet => T("연합함대", "連合艦隊", "Combined fleet");
	public static string KcvCombinedNotFormed => T("연합함대가 편성되어 있지 않습니다.", "連合艦隊は編成されていません。", "No combined fleet is formed.");
	public static string KcvCombinedFleetTab(string type) => $"{KcvCombinedFleet} · {type}";

	// the fleet panel's docked ships and the panel switch (mockup 20261009-fleet-dock-badge-v5)
	public static string KcvFleetDockBadge => T("입거", "入渠", "Dock");
	public static string KcvFleetDockHighlight => T("입거 시 강조", "入渠時に強調", "Highlight ships in dock");
	public static string KcvFleetDockHighlightToolTip => T(
		"입거 중인 함선의 줄을 입거 색으로 강조합니다.",
		"入渠中の艦の行を入渠の色で強調します。",
		"Highlights the rows of the ships in dock in the docking colour.");
	public static string KcvFleetSwitchOnComposition => T("편성 변경 시 패널 자동 전환", "編成変更時にパネルを自動切替", "Switch to the fleet panel on composition changes");
	public static string KcvFleetSwitchOnCompositionToolTip => T(
		"함대 편성을 바꾸거나 프리셋을 불러오면, 도킹된 그 함대 패널을 같은 칸의 맨 앞으로 꺼냅니다. 접힌 패널, 떠 있는 창, 닫힌 패널은 그대로입니다.",
		"艦隊の編成を変えるかプリセットを読み込むと、ドッキングされたその艦隊のパネルを同じ枠の一番前に出します。折りたたまれたパネル、フローティングウィンドウ、閉じたパネルはそのままです。",
		"When a fleet's composition changes or a preset is loaded, its docked fleet panel comes to the front of its pane. Collapsed panels, floating windows and closed panels stay as they are.");

	// the screenshots: the viewer's key, the settings' key box and the screenshot panel (mockup 20261009-screenshot-capture-v2)
	public static string Screenshot => T("스크린샷", "スクリーンショット", "Screenshot");
	public static string ScreenshotMenu => T("스크린샷(_X)", "スクリーンショット", "Screenshot");
	public static string ScreenshotButtonToolTip(string hotkey) => hotkey == ""
		? T("게임 화면을 찍습니다", "ゲーム画面を撮影します", "Takes a screenshot of the game")
		: T($"게임 화면을 찍습니다 ({hotkey})", $"ゲーム画面を撮影します ({hotkey})", $"Takes a screenshot of the game ({hotkey})");
	public static string ScreenshotHotkey => T("단축키", "ショートカットキー", "Shortcut key");
	// the label in the viewer's own settings page (style off), after its 「저장 방법 :」
	public static string ScreenshotHotkeyLabel => T("단축키 :", "ショートカットキー：", "Shortcut key:");
	public static string ScreenshotHotkeyWaiting => T("키를 누르세요", "キーを押してください", "Press a key");
	public static string ScreenshotHotkeyNone => T("없음", "なし", "None");
	public static string ScreenshotHotkeyClear => T("지우기", "クリア", "Clear");
	public static string ScreenshotHotkeyToolTip => T(
		"칸을 누른 뒤 쓸 키를 누릅니다(예: F2, Ctrl+Shift+S). 접속기 창이 앞에 있으면 어느 창에서나 게임 화면을 찍습니다. [지우기]를 누르면 단축키를 쓰지 않습니다.",
		"枠をクリックしてから使うキーを押します(例: F2、Ctrl+Shift+S)。ビューアーのウィンドウが前面にあれば、どのウィンドウからでもゲーム画面を撮影します。[クリア]を押すとショートカットキーを使いません。",
		"Click the box, then press the key to use (e.g. F2, Ctrl+Shift+S). While a window of the viewer is in front, the key takes a screenshot of the game from any window. [Clear] turns the key off.");
	public static string ScreenshotNoBrowser => T(
		"브라우저가 실행 중이 아니어서 스크린샷을 찍을 수 없습니다.",
		"ブラウザーが起動していないため、スクリーンショットを撮影できません。",
		"The browser is not running, so no screenshot can be taken.");
	public static string ScreenshotFailed(string message) => T(
		$"스크린샷을 찍지 못했습니다: {message}",
		$"スクリーンショットを撮影できませんでした: {message}",
		$"Could not take a screenshot: {message}");
	public static string ScreenshotHotkeyTaken(string hotkey) => T(
		$"스크린샷 단축키 {hotkey}을(를) 다른 프로그램이 쓰고 있어 등록하지 못했습니다.",
		$"スクリーンショットのショートカットキー {hotkey} は他のプログラムが使用中のため登録できませんでした。",
		$"The screenshot key {hotkey} is used by another program and could not be registered.");
	// The MapHP window (tools menu), after KanColleViewer's MapHP plugin; its name is the same in every language
	public static string KcvMapHp => "MapHP";
	public static string KcvMapHpHeader => T("공략 중인 해역", "攻略中マップ", "Maps in progress");
	public static string KcvMapHpTransport => T("함대 수송력", "艦隊の輸送力", "Fleet transport");
	public static string KcvMapHpNote => T("※ 출격(해역 선택) 화면을 열 때 갱신됩니다.", "※出撃(海域選択)画面を開いた際に更新されます。", "* Updated when the sortie (map selection) screen is opened.");
	// The item details window (tools menu, mockup 20261024-item-detail-v2)
	public static string KcvItemDetail => T("아이템 상세", "アイテム詳細", "Item details");
	public static string KcvItemDetailItem => T("아이템", "アイテム", "Item");
	public static string KcvItemDetailCount => T("보유 수", "保有数", "Owned");
	// The land base panel and the land base details window (KcvBaseAirCorpsView, KcvBaseAirCorpsDetailWindow, mockup 5)
	public static string KcvAirBaseDetail => T("기지 항공대 상세", "基地航空隊詳細", "Land base details");
	public static string KcvAirBaseNone => T("기지 항공대가 없습니다", "基地航空隊はありません", "No land base");
	public static string KcvAirColumnCorps => T("항공대", "航空隊", "Base");
	public static string KcvAirColumnAction => T("행동", "行動", "Action");
	public static string KcvAirColumnPower => T("제공", "制空", "Air");
	public static string KcvAirColumnDistance => T("거리", "半径", "Range");
	public static string KcvAirColumnSquadron(int n) => T($"{n}중대", $"第{n}中隊", $"Sq. {n}");
	public static string KcvAirDefenseValues(int defense, int highAltitude) => T($"방공 {defense} · 대고고도 {highAltitude}", $"防空 {defense} · 対高高度 {highAltitude}", $"Air defense {defense} · High altitude {highAltitude}");
	public static string KcvAirStates(int supremacy, int superiority, int parity, int denial) => T(
		$"확보: {supremacy} · 우세: {superiority} · 균형: {parity} · 열세: {denial}",
		$"確保: {supremacy} · 優勢: {superiority} · 均衡: {parity} · 劣勢: {denial}",
		$"Supremacy: {supremacy} · Superiority: {superiority} · Parity: {parity} · Denial: {denial}");
	public static string KcvAirCorpsCount(int n) => T($"항공대: {n}", $"航空隊: {n}", $"Bases: {n}");
	public static string KcvAirDefensePower(int n) => T($"방공 제공치: {n}", $"防空制空値: {n}", $"Air defense power: {n}");
	public static string KcvAirHighAltitude(int n) => T($"대고고도: {n}", $"対高高度: {n}", $"High altitude: {n}");
	public static string KcvAirStateReady => T("출격 준비 완료", "出撃準備完了", "Ready to sortie");
	public static string KcvAirStatePreparing => T("출격 준비 중", "出撃準備中", "Getting ready");
	public static string KcvAirStateDefense => T("방공 배치 중", "防空配置中", "On air defense");
	public static string KcvAirStateIdle(string action) => T($"{action} 중", $"{action}中", action);
	public static string KcvAirCheckSupply(bool ok) => ok
		? T("보급이 필요한 중대가 없습니다", "補給が必要な中隊はありません", "No squadron needs resupply")
		: T("보급이 필요한 중대가 있습니다", "補給が必要な中隊があります", "A squadron needs resupply");
	public static string KcvAirCheckFatigue(bool ok) => ok
		? T("피로한 중대가 없습니다", "疲労している中隊はありません", "No squadron is fatigued")
		: T("피로한 중대가 있습니다", "疲労している中隊があります", "A squadron is fatigued");
	public static string KcvAirCheckRelocating(bool ok) => ok
		? T("배치 전환 중인 중대가 없습니다", "配置転換中の中隊はありません", "No squadron is relocating")
		: T("배치 전환 중인 중대가 있습니다", "配置転換中の中隊があります", "A squadron is relocating");
	public static string KcvAirCheckEmpty(bool ok) => ok
		? T("빈 칸이 없습니다", "空きスロットはありません", "No empty slot")
		: T("빈 칸이 있습니다", "空きスロットがあります", "A slot is empty");
	public static string KcvAirDistance => T("거리", "半径", "Range");
	public static string KcvAirPowerLabel => T("제공", "制空", "Air");
	public static string KcvAirDefenseLabel => T("방공", "防空", "Defense");
	public static string KcvAirPlanesUnit => T("기", "機", "");
	public static string KcvAirStatusSupply => T("보급 필요", "要補給", "Needs resupply");
	public static string KcvAirStatusTired => T("피로", "疲労", "Fatigued");
	public static string KcvAirStatusReady => T("준비 완료", "準備完了", "Ready");
	public static string KcvAirStatusRelocating => T("배치 전환 중", "配置転換中", "Relocating");
	public static string KcvAirConditionNormal => T("보통", "通常", "Normal");
	public static string KcvAirTiredMark => T("(피로)", "(疲労)", "(fatigued)");
	public static string KcvAirEmptySlot => T("(빈 칸)", "(空き)", "(empty)");
	public static string KcvAirRelocatingSlot => T("(배치 전환 중)", "(配置転換中)", "(relocating)");
	// The fleet preset panel (KcvFleetPresetView); the lowest condition header is the viewer's own text
	public static string KcvPresetColumnName => T("이름", "名前", "Name");
	public static string KcvPresetNone => T("게임에서 편성 기록 화면을 한 번 열면 프리셋이 나옵니다.", "ゲームで編成記録の画面を一度開くとプリセットが表示されます。", "Open the fleet presets in the game once to show them here.");
	// The planned upgrades panel and the upgrade planner window (KcvEquipmentUpgradePlanView, KcvEquipmentUpgradePlannerWindow)
	public static string KcvUpgradeNextCost => T("다음 단계 필요 자재", "次の改修に必要な資材", "Next step cost");
	public static string KcvUpgradeHelpers => T("보조함", "二番艦", "Helpers");
	public static string KcvUpgradeHelpersToday => T("보조함 (오늘 개수 가능)", "二番艦 (本日改修可能)", "Helpers (today)");
	// the panel's button to the planner window: the window's name, as the tools menu has it (mockup 20261028-upgrade-plan-button-v1)
	public static string KcvUpgradePlanner => EquipmentUpgradePlannerResources.Title;
	public static string KcvUpgradeDayHelpers(string day) => T($"{day} 보조함", $"{day}の二番艦", $"{day} helpers");
	public static string KcvTotalLevel => T("합계 레벨", "合計レベル", "Total level");
	public static string KcvAverageLevel => T("평균 레벨", "平均レベル", "Average level");
	public static string KcvLos => T("색적치", "索敵値", "LoS");
	public static string KcvFighterPowerMin => T("제공치 (최소)", "制空値 (最小)", "Fighter power (min)");
	public static string KcvFighterPowerMax => T("제공치 (최대)", "制空値 (最大)", "Fighter power (max)");
	public static string KcvFleetSpeed(string speed) => T($"{speed} 함대", $"{speed}艦隊", $"{speed} fleet");
	public static string KcvStateSortie => T("함대가 출격 중입니다!", "艦隊は出撃中です！", "The fleet is on a sortie!");
	public static string KcvStateExpedition => T("함대가 원정 중입니다", "艦隊は遠征中です", "The fleet is on an expedition");
	public static string KcvStateReady => T("출격 가능!", "出撃可能！", "Ready to sortie!");
	public static string KcvStateNotReady => T("출격 준비 중", "出撃準備中", "Getting ready");
	public static string KcvCheckReady(bool ok) => ok
		? T("출격 준비 완료!", "出撃準備完了！", "Ready to sortie!")
		: T("출격 준비가 끝나지 않았습니다", "出撃準備が完了していません", "Not ready to sortie");
	public static string KcvCheckExpedition(bool ok) => ok
		? T("함대가 원정 중이 아닙니다", "艦隊は遠征中ではありません", "The fleet is not on an expedition")
		: T("함대가 원정 중입니다", "艦隊は遠征中です", "The fleet is on an expedition");
	public static string KcvCheckDamage(bool ok) => ok
		? T("함대에 큰 피해를 입은 함선이 없습니다", "艦隊の艦娘に大きなダメージはありません", "No ship is heavily damaged")
		: T("대파한 함선이 있습니다", "大破している艦娘がいます", "A ship is heavily damaged");
	public static string KcvCheckDock(bool ok) => ok
		? T("입거 중인 함선이 없습니다", "艦隊の艦娘は入渠していません", "No ship is in the dock")
		: T("입거 중인 함선이 있습니다", "入渠中の艦娘がいます", "A ship is in the dock");
	public static string KcvCheckSupply(bool ok) => ok
		? T("함대가 보급을 마쳤습니다", "艦隊は補給済みです", "The fleet is resupplied")
		: T("보급이 필요한 함선이 있습니다", "補給が必要な艦娘がいます", "A ship needs resupply");
	public static string KcvCheckFatigue(bool ok) => ok
		? T("피로한 함선이 없습니다", "艦隊の艦娘は疲労していません", "No ship is fatigued")
		: T("피로한 함선이 있습니다", "疲労している艦娘がいます", "A ship is fatigued");
	public static string KcvCheckComposition(bool ok) => ok
		? T("함대 편성이 정상입니다", "艦隊編成は正常です", "The fleet is composed normally")
		: T("함대에 함선이 없습니다", "艦隊に艦娘がいません", "The fleet has no ship");
	public static string KcvHealthy => T("건재", "健在", "Healthy");
	public static string KcvLightDamage => T("소파", "小破", "Light damage");
	public static string KcvModerateDamage => T("중파", "中破", "Moderate damage");
	public static string KcvHeavyDamage => T("대파", "大破", "Heavy damage");
	public static string KcvSunk => T("굉침", "轟沈", "Sunk");
	public static string KcvInDock => T("입거 중", "入渠中", "In dock");
	public static string KcvEscaped => T("퇴피", "退避", "Escaped");
	public static string KcvDamageControl => T("응급수리 탑재", "ダメコン搭載", "Damage control");
	public static string KcvProficiency(int level) => T($"(숙련도 {level})", $"(熟練度 {level})", $"(Proficiency {level})");
	public static string KcvExpeditionPanel => T("원정", "遠征", "Expedition");
	// the panel menu's item; the access key is for Korean screens only (user decision 2026-10-02)
	public static string KcvExpeditionMenu => T("원정(_E)", "遠征", "Expedition");
	// The expedition panel's table (as the dock and arsenal tables)
	public static string KcvExpeditionInProgress => T("진행 중인 원정", "遂行中の遠征", "Expedition in progress");
	public static string KcvFleetIdle => T("대기 중", "待機中", "Idle");
	public static string KcvFleetLocked => T("함대 미개방", "艦隊未開放", "Fleet locked");
	public static string KcvBattleInfo => T("전투 정보", "戦闘情報", "Battle info");
	// Column heads of a combined fleet battle
	public static string KcvFriendFleet1 => T("아군 1함대", "第一艦隊", "Friendly 1st");
	public static string KcvFriendFleet2 => T("아군 2함대", "第二艦隊", "Friendly 2nd");
	public static string KcvEnemyFleet1 => T("적군 1함대", "敵第一艦隊", "Enemy 1st");
	public static string KcvEnemyFleet2 => T("적군 2함대", "敵第二艦隊", "Enemy 2nd");
	// The compass panel
	public static string KcvMapArea => T("해역", "海域", "Map");
	public static string KcvExercise => T("연습", "演習", "Exercise");
	public static string KcvEnemyFleets => T("적 편성", "敵編成", "Enemy fleets");
	public static string KcvEnemyFleet => T("적 함대", "敵艦隊", "Enemy fleet");
	public static string KcvEscortFleet => T("호위 함대", "随伴艦隊", "Escort fleet");
	public static string KcvAirPower => T("제공", "制空", "Air");
	public static string KcvEndNode => T("종점", "終点", "End node");
	// The fleet overview
	public static string KcvHorizontalLayout => T("가로 배치", "横に並べる", "Side by side");
	public static string KcvVerticalLayout => T("세로 배치", "縦に並べる", "One under another");
	// the dock, arsenal and expedition panels (mockup 20261006-timer-panels-v2): the time left once it is over
	public static string KcvComplete => T("완료!", "完了!", "Done!");
	public static string KcvShowExpeditionName => T("원정명을 표시", "遠征名を表示", "Show expedition names");
	public static string KcvAnchorageRepairTimer => T("박지수리 타이머", "泊地修理タイマー", "Anchorage repair timer");
	public static string KcvHomePortSupplyTimer => T("모항급량 타이머", "母港給糧タイマー", "Home port supply timer");
	// The headquarters' context menu and 임의 아이템 (mockup 20261005-headquarters-icons-v4)
	public static string KcvShow => T("표시", "表示", "Show");
	public static string KcvHqNoItem => T("없음", "なし", "None");
	public static string KcvHqHidden => T("숨김", "非表示", "hidden");
	public static string KcvHqItemHint => T("(우클릭 메뉴에서 아이템 변경 가능)", "(右クリックメニューでアイテム変更可能)", "(The item can be changed from the context menu)");
	public static string KcvReturnTime(string time) => T($"귀환 일시: {time}", $"帰還日時: {time}", $"Returns: {time}");
	public static string KcvLoS => T("색적", "索敵", "LoS");
	public static string KcvDrums(int count, int ships) => T($"드럼통 탑재: {count}개 (장착 {ships})", $"ドラム缶搭載: {count}個 (装備 {ships})", $"Drums: {count} (on {ships})");
	public static string KcvLandingCraft(int count, int ships) => T($"대발동정 탑재: {count}개 (장착 {ships})", $"大発動艇搭載: {count}個 (装備 {ships})", $"Landing craft: {count} (on {ships})");
	public static string KcvSurfaceRadar(int count, int ships) => T($"수상 전탐: {count}개 (장착 {ships})", $"水上電探: {count}個 (装備 {ships})", $"Surface radars: {count} (on {ships})");
	public static string KcvTransportTp(int s, int a) => T($"수송 TP: S {s} / A {a}", $"輸送TP: S {s} / A {a}", $"Transport TP: S {s} / A {a}");
	public static string KcvTankTp => T("전차 TP", "戦車TP", "Tank TP");
	public static string KcvAirPowerTotal(int air) => T($"제공 전력 합계: {air}", $"制空戦力合計: {air}", $"Air power total: {air}");
	public static string KcvLosHeader => T("색적 (Cn 33식):", "索敵 (Cn 33式):", "LoS (Cn, formula 33):");
	public static string KcvLosLine(int cn, double los) => T($"　분기점계수{cn}: {los:f2}", $"　分岐点係数{cn}: {los:f2}", $"　Cn {cn}: {los:f2}");
	public static string KcvSpriteFailed(string sprite, string message) => T(
		$"제독업무 스타일: 게임 그림 {sprite}을(를) 읽지 못했습니다({message}).",
		$"KanColleViewer style: the game image {sprite} could not be read ({message}).");

	// The game's files the viewer draws itself, fetched when missing (ForkGameAssets)
	public static string GameAssetsFetching(int count, string server) => T(
		$"게임 그림 묶음 {count}개가 없어 미리 받습니다({server}).",
		$"{count} game sprites are missing; fetching them ({server}).");
	public static string GameAssetsNoServer(int count) => T(
		$"게임 그림 묶음 {count}개가 없지만 받을 서버가 비어 있어 받지 않습니다(Fork.json Assets.DownloadServer).",
		$"{count} game sprites are missing; no server to fetch them from (Fork.json Assets.DownloadServer).");
	public static string GameAssetFetched(string file, int bytes) => T($"게임 그림을 받았습니다: {file} ({bytes / 1024:N0} KB)", $"Fetched {file} ({bytes / 1024:N0} KB)");
	public static string GameAssetFetchFailed(string file, string message) => T(
		$"게임 그림을 받지 못했습니다: {file} ({message}). 다음 실행 때 다시 받습니다.",
		$"Could not fetch {file} ({message}); it is fetched again at the next start.");
	public static string GameAssetNotGameFile => T("게임 파일이 아닌 응답", "not a game file");

	// The translations' own address (ForkSettings.Translation, SoftwareUpdater; mockup 20261007-translation-url-v2)
	public static string TranslationUrlEmpty => T("번역 주소가 비어 있어 번역을 받지 않습니다.", "翻訳アドレスが空のため、翻訳を取得しません。", "The translation address is empty; no translations are fetched.");
	public static string TranslationUpdateFailed(string url, string message) => T(
		$"번역 주소에서 번역 판 정보를 받지 못해 지금 있는 번역을 그대로 씁니다({url}: {message}).",
		$"翻訳アドレスから翻訳のバージョン情報を取得できなかったため、今の翻訳をそのまま使います({url}: {message})。",
		$"Could not read the translations' versions from the translation address; the current translations stay ({url}: {message}).");
	public static string TranslationUpdateEmpty => T("빈 응답", "空の応答", "empty response");
	public static string KcvConfigTranslationUrl => T("번역 주소", "翻訳アドレス", "Translation address");
	public static string KcvConfigTranslationUrlToolTip => T(
		"함선·장비·임무·원정·해역 이름의 한국어 번역을 받을 저장소 주소입니다. 비우면 번역을 받지 않습니다.",
		"艦・装備・任務・遠征・海域名の韓国語翻訳を取得するリポジトリのアドレスです。空にすると翻訳を取得しません。",
		"The repository the Korean translations of ship, equipment, quest, expedition and map names come from. Empty: no translations are fetched.");
	// The start-up update checks (ForkUpdateWatch, mockup 20261007-update-watch-v1): the boxes' and alarms' titles
	public static string UpdateWatchGame => T("게임 업데이트 감지", "ゲーム更新の検知", "Game update check");
	public static string UpdateWatchGameToolTip => T(
		"접속기를 시작할 때 공식 서버의 게임 버전을 한 번 확인해, 지난번과 다르면 알람을 띄웁니다.",
		"起動時に公式サーバーのゲームバージョンを一度確認し、前回と違えば通知します。",
		"At start, checks the game version on the official server once and raises an alarm when it differs from the last one.");
	public static string UpdateWatchKc3 => T("kc3 데이터 업데이트 감지", "kc3 データ更新の検知", "kc3 data update check");
	public static string UpdateWatchKc3ToolTip => T(
		"접속기를 시작할 때 KC3 번역 저장소의 최신 커밋을 한 번 확인해, 지난번과 다르면 알람을 띄웁니다.",
		"起動時に KC3 翻訳リポジトリの最新コミットを一度確認し、前回と違えば通知します。",
		"At start, checks the newest commit of KC3's translations once and raises an alarm when it differs from the last one.");
	public static string UpdateWatchGameMessage(string old, string now) => T($"게임 버전 {old} → {now}", $"ゲームバージョン {old} → {now}", $"Game version {old} → {now}");
	public static string UpdateWatchKc3Message(string old, string now) => T($"KC3 커밋 {old} → {now}", $"KC3 コミット {old} → {now}", $"KC3 commit {old} → {now}");
	public static string UpdateWatchFirst(string what, string value) => T(
		$"{what}: 처음 확인한 값을 저장했습니다({value}). 다음 시작부터 비교합니다.",
		$"{what}: 初回の値を保存しました({value})。次回の起動から比較します。",
		$"{what}: kept the first value ({value}); later starts compare with it.");
	public static string UpdateWatchFailed(string what, string message) => T($"{what}: 확인하지 못했습니다({message}).", $"{what}: 確認できませんでした({message})。", $"{what}: could not check ({message}).");
	public static string KcvConfigDataUrl => T("데이터 주소", "データアドレス", "Data address");
	public static string KcvConfigDataUrlToolTip => T(
		"임무 진행 추적·출격 잠금·장비 보너스·개수 공창 데이터를 받을 저장소 주소입니다(74식 데이터).",
		"任務進捗の追跡・出撃札・装備ボーナス・改修工廠のデータを取得するリポジトリのアドレスです(74式のデータ)。",
		"The repository quest trackers, sortie locks, equipment bonuses and improvement data come from (EO's data).");

	public static string KcvWinFormsError(string message) => T($"제독업무 스타일(WinForms): {message}", $"KanColleViewer style (WinForms): {message}");

	// The daily log cleanup (ForkLogCleanup)
	public static string LogCleanupHour => T("로그 정리 시각", "ログ整理の時刻", "Log cleanup time");
	public static string LogCleanupHourToolTip(int keep) => T(
		$"매일 이 시각에 최근 {keep:N0}줄보다 오래된 로그를 로그 파일에 덮어써 저장하고 메모리에서 지웁니다(로그 저장이 꺼져 있으면 지우기만 합니다). 종료할 때도 남은 로그로 파일을 덮어씁니다. 로그 창에는 최근 {keep:N0}줄만 보입니다.",
		$"毎日この時刻に、最新 {keep:N0} 行より古いログをログファイルに上書き保存し、メモリから消去します(ログ保存がオフなら消去のみ)。終了時も残りのログでファイルを上書きします。ログ画面には最新 {keep:N0} 行だけを表示します。",
		$"Every day at this time, log entries older than the newest {keep:N0} are written to the log file, overwriting it, and dropped from memory (only dropped when saving the log is off). Closing the viewer overwrites the file with the entries left. The log panel shows the newest {keep:N0} lines.");
	public static string LogCleanedUp(int count) => T(
		$"오래된 로그 {count:N0}줄을 로그 파일에 저장하고 메모리에서 지웠습니다.",
		$"古いログ {count:N0} 行をログファイルに保存し、メモリから消去しました。",
		$"{count:N0} old log entries were written to the log file and dropped from memory.");
	public static string LogCleanupFailed(string message) => T(
		$"로그 정리: 로그 파일에 쓰지 못해 메모리의 로그를 그대로 둡니다({message}).",
		$"ログ整理: ログファイルに書き込めなかったため、メモリのログを残します({message})。",
		$"Log cleanup: the log file could not be written, so the entries stay in memory ({message}).");

	public static string PluginsNotLoaded => T("플러그인을 읽지 않았습니다(--no-plugins).", "Plugins are not loaded (--no-plugins).");
	public static string PluginLoaded(string? name, string? version) => T($"플러그인을 읽었습니다: {name} {version}", $"Plugin loaded: {name} {version}");
	public static string PluginFailed(string folder, string message) => T($"플러그인을 읽지 못했습니다({folder}): {message}", $"Plugin failed ({folder}): {message}");
}
