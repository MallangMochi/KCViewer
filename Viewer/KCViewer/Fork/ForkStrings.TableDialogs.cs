namespace KCViewer.Fork;

/// <summary>
/// Fork: the table dialogs in KanColleViewer's style (KcvTableDialogs, mockup 20261017-table-dialogs-v2): 열 표시 설정,
/// 데이터 그리드 설정 and the ship group's name. The user's words of 2026-10-17 where they changed the viewer's.
/// </summary>
public static partial class ForkStrings
{
	public static string ColumnSelectorShown => T("표시 열", "表示する列", "Columns shown");
	public static string ColumnSelectorFromLeft => T("왼쪽부터", "左から", "From the left");
	public static string ColumnSelectorColumns => T("열", "列", "columns");
	public static string ColumnSelectorFrozenNote => T("항상 표시될 열의 수", "常に表示される列の数", "Columns always shown");

	public static string GridSettingsHeights => T("높이", "高さ", "Heights");
	public static string GridSettingsHeader => T("머리줄 높이", "ヘッダーの高さ", "Header height");
	public static string GridSettingsRow => T("줄 높이", "行の高さ", "Row height");
	public static string GridSettingsNote => T(
		"10~50. 바꾸면 바로 표에 보이고, 취소하면 되돌립니다.",
		"10～50。変えるとすぐ表に反映され、キャンセルで元に戻ります。",
		"10 to 50. The table shows a change at once; Cancel puts it back.");

	public static string GroupNameBox => T("그룹", "グループ", "Group");
	public static string GroupName => T("그룹 이름", "グループ名", "Group name");

	/// <summary>The copy's title as the menu's (the viewer's Korean was 「그룹을 복사」).</summary>
	public static string GroupCopyTitle => T("그룹 복사", "グループをコピー", "Copy group");
}
