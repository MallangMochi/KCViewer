namespace KCViewer.Fork;

/// <summary>
/// Fork: the ship group's filter in KanColleViewer's style (KcvShipGroupFilterWindow, mockup 20261019-group-filter-v6,
/// approved 2026-10-19 "구현"). The comparisons and the expression's words are the user's (2026-10-19: 「~포함 / ~포함
/// 안 함 / 접두사/접두사 아님 / 접미사/접미사 아님 / AND/OR/NOT/(없음)」, 「접두사 「후부」」); the values and notes the
/// viewer wrote in Japanese are in Korean. The other labels come from the viewer's resources.
/// </summary>
public static partial class ForkStrings
{
	public static string FilterConditions => T("조건", "条件", "Conditions");
	public static string FilterBlockName => T("묶음 {0}", "ブロック {0}", "Block {0}");
	public static string FilterAddCondition => T("＋ 조건", "＋ 条件", "＋ Condition");
	public static string FilterAddBlock => T("＋ 묶음", "＋ ブロック", "＋ Block");
	public static string FilterMenu => T("메뉴 ▾", "メニュー ▾", "Menu ▾");
	public static string FilterConfirmTitle => T("확인", "確認", "Confirmation");
	public static string FilterNone => T("(없음)", "(なし)", "(none)");

	public static string FilterOpEqual => T("같음", "等しい", "equal");
	public static string FilterOpNotEqual => T("같지 않음", "等しくない", "not equal");
	public static string FilterOpLessThan => T("미만", "未満", "less than");
	public static string FilterOpLessEqual => T("이하", "以下", "at most");
	public static string FilterOpGreaterThan => T("초과", "超過", "more than");
	public static string FilterOpGreaterEqual => T("이상", "以上", "at least");
	public static string FilterOpContains => T("포함", "含む", "contains");
	public static string FilterOpNotContains => T("포함 안 함", "含まない", "does not contain");
	public static string FilterOpBeginWith => T("접두사", "前方一致", "starts with");
	public static string FilterOpNotBeginWith => T("접두사 아님", "前方一致しない", "does not start with");
	public static string FilterOpEndWith => T("접미사", "後方一致", "ends with");
	public static string FilterOpNotEndWith => T("접미사 아님", "後方一致しない", "does not end with");
	public static string FilterOpHas => T("있음", "あり", "has");
	public static string FilterOpHasNot => T("없음", "なし", "has not");

	// {0} the item, {1} the value
	public static string FilterTextContains => T("{0} 「{1}」 포함", "{0} 「{1}」 を含む", "{0} contains 「{1}」");
	public static string FilterTextNotContains => T("{0} 「{1}」 포함 안 함", "{0} 「{1}」 を含まない", "{0} does not contain 「{1}」");
	public static string FilterTextBeginWith => T("{0} 접두사 「{1}」", "{0} 前方一致 「{1}」", "{0} starts with 「{1}」");
	public static string FilterTextNotBeginWith => T("{0} 접두사 「{1}」 아님", "{0} 前方一致 「{1}」 でない", "{0} does not start with 「{1}」");
	public static string FilterTextEndWith => T("{0} 접미사 「{1}」", "{0} 後方一致 「{1}」", "{0} ends with 「{1}」");
	public static string FilterTextNotEndWith => T("{0} 접미사 「{1}」 아님", "{0} 後方一致 「{1}」 でない", "{0} does not end with 「{1}」");
	public static string FilterTextHas => T("{0} 「{1}」 있음", "{0} 「{1}」 あり", "{0} has 「{1}」");
	public static string FilterTextHasNot => T("{0} 「{1}」 없음", "{0} 「{1}」 なし", "{0} has not 「{1}」");

	public static string FilterValueNone => T("(없음)", "(なし)", "(none)");
	public static string FilterValueLocked => T("(미개방)", "(未開放)", "(locked)");
	public static string FilterValueNotEnrolled => T("(재적 없음)", "(未在籍)", "(not enrolled)");
	public static string FilterValueNoRemodelBefore => T("(개장 전 없음)", "(未改装)", "(not remodelled)");
	public static string FilterValueLastRemodel => T("(마지막 개장)", "(最終改装)", "(final remodel)");
	public static string FilterDockNote => T("-1=입거 안 함, 1~4=입거 중(도크 번호)", "-1=未入渠, 1～4=入渠中(ドック番号)", "-1 = not docked, 1-4 = docked (dock number)");
	public static string FilterMillisecondsNote => T("(밀리초 단위) {0}", "(ミリ秒単位) {0}", "(milliseconds) {0}");
}
