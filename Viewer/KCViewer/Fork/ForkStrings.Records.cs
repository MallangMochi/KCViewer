using System.Collections.Generic;

namespace KCViewer.Fork;

/// <summary>
/// Fork: text of the record windows in KanColleViewer's style (Fork/KcvViews/KcvRecord*, VIEWER-PLAN.md "기록 묶음").
/// The labels the viewer already has keep coming from its own resources; these are the new ones.
/// </summary>
public static partial class ForkStrings
{
	public static string KcvRecordConditions => T("검색 조건", "Search conditions");
	public static string KcvRecordFold => T("접기 ▴", "Fold ▴");
	/// <summary>The box of the anti-air defense window (mockup 20261006-calc-windows-v2), which has no fold.</summary>
	public static string KcvCalcConditions => T("계산 조건", "計算条件", "Conditions");
	// the expedition calculator (mockup 20261006-expedition-calculator-v2)
	public static string KcvScoreWeights => T("점수 가중치", "スコアの重み", "Score weights");
	public static string KcvExpeditionName => T("원정 이름", "遠征名", "Expedition");
	// the fleet image generator (mockup 20261007-fleet-image-v2): its boxes, the settings window and the theme's labels
	public static string KcvFleetImageConditions => T("출력 조건", "出力条件", "Output");
	public static string KcvFleetImageFleets => T("함대", "艦隊", "Fleets");
	public static string KcvFleetImageImage => T("이미지", "画像", "Image");
	public static string KcvFleetImageSave => T("저장", "保存", "Save");
	public static string KcvFleetImageOther => T("그 밖", "その他", "Other");
	public static string KcvFleetImageFontSizes => T("폰트 크기", "フォントサイズ", "Font sizes");
	public static string KcvFleetImageSettings => T("편성 이미지 설정", "編成画像の設定", "Fleet image settings");
	public static string KcvFleetImageForeColor => T("글자색", "文字色", "Text colour");
	public static string KcvFleetImageBackColor => T("바탕색", "背景色", "Background colour");
	// the event lock planner (mockup 20261007-event-lock-v2): its boxes, cards and the filter's equipment row
	public static string KcvLockPhases => T("단계", "フェーズ", "Phases");
	public static string KcvLockLocks => T("딱지", "札", "Locks");
	public static string KcvLockFreeShips => T("딱지 없는 함선", "札なしの艦", "Ships without a lock");
	/// <summary>A phase card's ship count, a binding's StringFormat.</summary>
	public static string KcvLockShipCount => T("{0}척", "{0}隻", "{0} ships");
	public static string KcvLockFinished => T("완료", "完了", "Done");
	public static string KcvLockDropHint => T("함선을 끌어다 놓기", "艦をドラッグして追加", "Drag ships here");
	public static string KcvLockEquipment => T("장비", "装備", "Equipment");
	// the settings window (mockup 20261007-settings-v2): the boxes' titles and labels
	public static string KcvConfigBrowserProxy => T("브라우저 프록시", "ブラウザのプロキシ", "Browser proxy");
	public static string KcvConfigSaveData => T("통신 내용 저장", "通信内容の保存", "Saving traffic");
	public static string KcvConfigAddress => T("주소", "アドレス", "Address");
	public static string KcvConfigPort => T("포트", "ポート", "Port");
	public static string KcvConfigSaveWhat => T("저장할 것", "保存するもの", "Save");
	public static string KcvConfigFonts => T("글꼴", "フォント", "Fonts");
	public static string KcvConfigDisplay => T("표시", "表示", "Display");
	public static string KcvConfigTranslation => T("번역", "翻訳", "Translation");
	public static string KcvConfigThemeLanguage => T("테마와 언어", "テーマと言語", "Theme and language");
	public static string KcvConfigLogFile => T("로그 파일", "ログファイル", "Log file");
	public static string KcvConfigSpoiler => T("스포일러", "ネタバレ", "Spoilers");
	public static string KcvConfigRecordDisplay => T("기록과 표시", "記録と表示", "Records and display");
	public static string KcvConfigWarnings => T("경고와 소리", "警告と音", "Warnings and sound");
	public static string KcvConfigTranslationData => T("번역 데이터", "翻訳データ", "Translation data");
	public static string KcvConfigLayout => T("레이아웃", "レイアウト", "Layout");
	public static string KcvConfigWindow => T("창", "ウィンドウ", "Window");
	public static string KcvConfigPage => T("페이지", "ページ", "Page");
	public static string KcvConfigFormat => T("형식", "形式", "Format");
	public static string KcvConfigBrowserRestart => T("브라우저 (다시 시작한 뒤 반영)", "ブラウザ (再起動後に反映)", "Browser (after a restart)");
	public static string KcvConfigShownItems => T("표시 항목", "表示項目", "Items shown");
	public static string KcvConfigDataSubmission => T("데이터 제출 (기본 꺼짐)", "データ送信 (既定はオフ)", "Data submission (off by default)");
	public static string KcvConfigDebugBox => T("디버그 (디버그 메뉴를 켰을 때)", "デバッグ (デバッグメニューが有効なとき)", "Debug (with the debug menu)");
	public static string KcvConfigApiListHint => T("API 목록 파일", "API リストのファイル", "API list file");
	public static string KcvConfigBgmPlayer => T("BGM 플레이어", "BGM プレイヤー", "BGM player");
	public static string KcvConfigBgmUse => T("사용", "使用", "Use");
	public static string KcvConfigBgmFile => T("파일", "ファイル", "File");
	public static string KcvConfigBgmNoFile => T("(없음)", "(なし)", "(none)");
	public static string KcvConfigSoundHint => T("소리 파일", "音声ファイル", "Sound file");
	public static string KcvConfigImageHint => T("이미지 파일", "画像ファイル", "Image file");
	// 알람 › 기본 설정, the common notification settings (ForkNotifierDefaults)
	public static string KcvNotifyDefault => T("기본 설정", "基本設定", "Defaults");
	public static string KcvNotifyOwn => T("이 알람만 개별 설정", "この通知だけ個別に設定", "Set for this notification only");
	public static string KcvNotifyFollows => T("기본 설정을 따름(값은 알람 › 기본 설정에서 바꿈)", "基本設定に従う (値は 通知 › 基本設定 で変更)", "Follows the defaults (change them in Notification › Defaults)");
	public static string KcvNotifyDefaultNote => T(
		"알람마다 「이 알람만 개별 설정」을 켜지 않은 곳에 이 값이 쓰입니다. 켠 곳은 그 알람의 값이 우선합니다.",
		"通知ごとに「この通知だけ個別に設定」をオンにしていない所にこの値が使われます。オンの所はその通知の値が優先します。",
		"These values are used wherever a notification does not have 'Set for this notification only' on; where it is on, the notification's own values win.");
	public static string KcvNotifyDefaultTest => T("기본 설정으로 시험 알람을 띄움", "基本設定でテスト通知を表示", "Shows a test notification with the defaults");
	public static string KcvNotifyByNotification => T("알람마다", "通知ごと", "By notification");
	public static string KcvNotifyFollowsShort => T("기본", "基本", "Default");
	public static string KcvNotifyOwnShort => T("개별", "個別", "Own");
	public static string KcvNotifyBaseAirCorps => T("기지 항공대", "基地航空隊", "Base air corps");
	public static string KcvNotifyBattleEnd => T("전투 종료", "戦闘終了", "Battle end");
	// the quest tracker manager (mockup 20261006-quest-tracker-v2): its buttons, boxes and the labels of its rows
	public static string KcvTrackerAdd => T("추적 추가", "追跡を追加", "Add tracker");
	public static string KcvTrackerRemove => T("삭제", "削除", "Remove");
	public static string KcvTrackerAddRow => T("＋ 추가", "＋ 追加", "＋ Add");
	public static string KcvTrackerTasks => T("조건", "条件", "Tasks");
	public static string KcvTrackerShipConditions => T("함선 조건", "艦船条件", "Ship conditions");
	public static string KcvTrackerMap => T("해역", "海域", "Map");
	public static string KcvTrackerRank => T("랭크", "ランク", "Rank");
	public static string KcvTrackerExpedition => T("원정", "遠征", "Expedition");
	public static string KcvTrackerEquipment => T("장비", "装備", "Equipment");
	public static string KcvTrackerCategory => T("분류", "分類", "Category");
	public static string KcvTrackerProgress => T("진행", "進捗", "Progress");
	public static string KcvTrackerGoal => T("목표", "目標", "Goal");
	public static string KcvTrackerNodeIds => T("칸 번호", "マス番号", "Node IDs");
	public static string KcvTrackerNodeName => T("이름", "名前", "Name");
	public static string KcvTrackerCardType => T("도감 종류", "図鑑種別", "Card type");
	public static string KcvTrackerIconType => T("아이콘", "アイコン", "Icon");
	public static string KcvTrackerShipClass => T("함급", "艦型", "Class");
	public static string KcvTrackerShip => T("함선", "艦船", "Ship");
	public static string KcvTrackerRemodel => T("개조", "改装", "Remodel");
	public static string KcvTrackerShipTypes => T("함종", "艦種", "Types");
	public static string KcvTrackerNationality => T("국적", "国籍", "Nationality");
	public static string KcvTrackerShipCount => T("수", "隻数", "Count");
	// the battle panel's dropped ship (mockup 20261006-battle-drop-v2)
	public static string KcvDrop => T("드랍", "ドロップ", "Drop");
	public static string KcvDropNone => T("없음", "なし", "None");
	public static string KcvDropSpoiler => T("스포일러", "ネタバレ", "Spoiler");
	public static string KcvShowDrop => T("드랍함 표시", "ドロップ艦を表示", "Show dropped ship");
	// 칸무스 수집 진행도 (mockup 20261006-kancolle-progress-v3): the group of ship types the viewer does not know, the owned total
	public static string KcvProgressOthers => T("그외", "その他", "Others");
	public static string KcvProgressOwned(int owned, int total)
	{
		double rate = total is 0 ? 0 : owned * 100.0 / total;
		return T($"보유 {owned} / {total}종 ({rate:0.0}%)", $"保有 {owned} / {total}種 ({rate:0.0}%)", $"Owned {owned} / {total} ({rate:0.0}%)");
	}
	public static string KcvRecordUnfold => T("펼치기 ▾", "Unfold ▾");
	public static string KcvRecordSearch => T("검색", "Search");
	public static string KcvRecordCancel => T("취소", "Cancel");
	public static string KcvRecordReset => T("초기화", "Reset");
	public static string KcvRecordPeriod => T("기간", "Period");
	public static string KcvRecordTime => T("시간", "Time");
	public static string KcvRecordRoute => T("경로", "Route");
	public static string KcvRecordRank => T("랭크", "Rank");
	public static string KcvRecordExpedition => T("원정", "Expedition");
	public static string KcvRecordCell => T("칸", "Node");
	public static string KcvRecordDifficulty => T("난이도", "Difficulty");
	public static string KcvRecordBoss => T("보스", "Boss");
	public static string KcvRecordFlagship => T("기함", "Flagship");
	public static string KcvRecordLarge => T("대형", "Large");

	// the construction table's development material columns, ×1 ×20 ×100 ("개발자재 1 20 100 순서로", 2026-10-04)
	public static string KcvRecordDevMats1 => T("개발 자재×1", "Dev. material ×1");
	public static string KcvRecordDevMats20 => T("개발 자재×20", "Dev. material ×20");
	public static string KcvRecordDevMats100 => T("개발 자재×100", "Dev. material ×100");

	// the drop record's ship picker (mockup v5 ③-1, 2026-10-05)
	public static string KcvPickerOptionsHint => T("누르면 그 조건으로 고르고 닫힘", "Click to pick that condition and close");
	public static string KcvPickerNameHint => T("이름 또는 읽기", "Name or reading");
	public static string KcvPickerShipType => T("함종", "Type");
	public static string KcvPickerShips(int count) => T($"{count}척", count == 1 ? "1 ship" : $"{count} ships");
	public static string KcvPickerRowHint => T("줄을 누르면 그 함선으로 고르고 닫힘", "Click a row to pick that ship and close");

	// the ship selector of 칸무스 레벨링's 추가, the quest tracker's ship conditions and the Discord ship (mockup
	// 20261008-ship-selector-v2): the level range (user's request 2026-10-08), the equipment's three states, 적용 중
	public static string KcvSelectorLevel => T("레벨", "Level");
	public static string KcvSelectorEquipment => T("장비:", "Equipment:");
	public static string KcvSelectorThreeStateHint => T("누를 때마다 상관없음 → 가능 → 불가", "Each click: any → can → cannot");
	public static string KcvSelectorCan => T("가능", "can");
	public static string KcvSelectorCannot => T("불가", "cannot");
	public static string KcvSelectorNoType => T("함종 없음", "No type");
	// the equipment selector (mockup 20261009-upgrade-plan-windows-v2): the game's categories, as the equipment list
	public static string KcvSelectorCategory => T("분류:", "Category:");
	public static string KcvSelectorNoCategory => T("분류 없음", "No category");
	public static string KcvSelectorProficiency => T("숙련", "Prof.");
	public static string KcvSelectorEquipmentCount(int count) => T($"{count}개", count == 1 ? "1 item" : $"{count} items");
	public static string KcvSelectorEquipmentRowHint => T("줄을 누르면 그 장비로 고르고 닫힘", "Click a row to pick that equipment and close");
	public static string KcvSelectorApplied(IEnumerable<string> conditions) => T($"적용 중: {string.Join(" · ", conditions)}", $"Applied: {string.Join(" · ", conditions)}");

	// Senka viewer
	public static string KcvSenkaInProgress => T("진행 중", "In progress");
	public static string KcvSenkaSessions(int count) => T($"{count}구간", count == 1 ? "1 session" : $"{count} sessions");
	public static string KcvSenkaTotalOfAll(int count) => T(
		$"전체 {count}구간의 합 (줄을 고르면 고른 줄들의 합)",
		$"Sum of all {count} sessions (select rows for their sum)");
	public static string KcvSenkaTotalOfSelected(int count) => T($"고른 {count}구간의 합", $"Sum of the {count} selected sessions");
	public static string KcvSenkaNoRecords => T("기간 안에 기록이 없습니다.", "There are no records in the period.");

	// Resource chart: the user's names for the eight items (2026-10-04)
	public static string KcvChartResources => T("자원", "Resources");
	public static string KcvChartMaterials => T("자재", "Materials");
	public static string KcvChartFuel => T("연료", "Fuel");
	public static string KcvChartAmmo => T("탄약", "Ammo");
	public static string KcvChartSteel => T("강재", "Steel");
	public static string KcvChartBauxite => T("보크사이트", "Bauxite");
	public static string KcvChartInstantRepair => T("고속수복재", "Instant repair");
	public static string KcvChartInstantConstruction => T("고속건조재", "Instant construction");
	public static string KcvChartDevelopmentMaterial => T("개발자재", "Development material");
	public static string KcvChartImprovementMaterial => T("개수자재", "Improvement material");
	public static string KcvChartExperience => T("경험치", "Experience");
	public static string KcvChartPerDay => T(" /일", " /day");

	/// <summary>A left axis label in 만 (10,000) as the user asked ("1.2만", "1,641만"), thousands elsewhere.</summary>
	public static string KcvChartTenThousands(double value) => IsKorean
		? (value == 0 ? "0" : $"{value / 10000:#,0.#}만")
		: (value == 0 ? "0" : $"{value / 1000:#,0.#}k");

	public static string KcvChartSaveFilter => T(
		"PNG 파일 (*.png)|*.png|JPG 파일 (*.jpg, *.jpeg)|*.jpg;*.jpeg|BMP 파일 (*.bmp)|*.bmp|모든 파일 (*.*)|*.*",
		"PNG files (*.png)|*.png|JPG files (*.jpg, *.jpeg)|*.jpg;*.jpeg|BMP files (*.bmp)|*.bmp|All files (*.*)|*.*");
}
