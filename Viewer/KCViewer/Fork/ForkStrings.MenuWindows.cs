namespace KCViewer.Fork;

/// <summary>
/// Fork: 자동 재로딩, UI 차단기 and 버전 정보 in KanColleViewer's style (KcvMenuWindows, mockup
/// 20261018-menu-windows-v2, approved 2026-10-18 "구현"). The viewer's labels (싱글 맵 모드, 위·왼쪽·높이·너비, 기본값,
/// 배경, 이미지, 미리 보기, 닫기) come from its own resources; no explanations of ours ("쓸데없는 설명문구 넣지 말것").
/// </summary>
public static partial class ForkStrings
{
	public static string AutoRefreshRules => T("진행 허용 칸", "進行を許可するマス", "Allowed cells");
	public static string AutoRefreshAddRule => T("＋ 규칙 추가", "＋ ルールを追加", "＋ Add rule");
	public static string AutoRefreshEnabled => T("사용", "使用", "Enabled");
	public static string AutoRefreshRemoveRule => T("규칙 삭제", "ルールを削除", "Remove rule");
	public static string AutoRefreshAllowedCells => T("허용된 칸", "許可したマス", "Allowed");
	public static string AutoRefreshAddCell => T("칸 더하기", "マスを追加", "Add cell");
	public static string AutoRefreshCellNumber => T("번호", "番号", "No.");
	public static string AutoRefreshNoCells => T("(없음)", "(なし)", "(none)");

	/// <summary>The status bar: 「규칙 2개(사용 1) · 싱글 맵 모드 꺼짐」 or 「… · 싱글 맵 모드 켜짐 1-1」.</summary>
	public static string AutoRefreshStatus(int rules, int enabled, string? singleMap) => singleMap is null
		? T($"규칙 {rules}개(사용 {enabled}) · 싱글 맵 모드 꺼짐", $"ルール {rules}個(使用 {enabled}) · シングルマップモード オフ", $"{rules} rules ({enabled} enabled) · single map mode off")
		: T($"규칙 {rules}개(사용 {enabled}) · 싱글 맵 모드 켜짐 {singleMap}", $"ルール {rules}個(使用 {enabled}) · シングルマップモード オン {singleMap}", $"{rules} rules ({enabled} enabled) · single map mode on {singleMap}");

	public static string UiBlockerTaiha => T("대파 진격 차단", "大破進撃ブロック", "Heavy damage advance block");

	public static string VersionViewer => T("접속기", "ビューア", "Viewer");
	public static string VersionOriginal => T("74식", "七四式", "Electronic Observer");
	public static string VersionRowVersion => T("버전", "バージョン", "Version");
	public static string VersionRowBuilt => T("빌드", "ビルド", "Built");
	public static string VersionRowBase => T("기반", "ベース", "Based on");
	public static string VersionRowRuntime => T("실행 환경", "実行環境", "Runtime");
	public static string VersionRowRepository => T("저장소", "リポジトリ", "Repository");
	public static string VersionRowDeveloper => T("개발", "開発", "Developed by");
	public static string VersionRowMaintainers => T("수정", "改修", "Modified by");
}
