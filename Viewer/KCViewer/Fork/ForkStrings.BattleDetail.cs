namespace KCViewer.Fork;

/// <summary>
/// Fork: text of the battle detail and the sortie detail in KanColleViewer's style (Fork/KcvViews/KcvBattleDetail*,
/// mockup 20261010-battle-sortie-detail-v2). The names the viewer already has (phases, fleets, results) keep coming
/// from its own resources; these are the new ones.
/// </summary>
public static partial class ForkStrings
{
	public static string KcvBattleSearching => T("색적·진형", "索敵・陣形", "Search and formation");
	public static string KcvBattleFormation => T("진형", "陣形", "Formation");
	public static string KcvBattleFriend => T("아군", "味方", "Friendly");
	public static string KcvBattleEnemy => T("적", "敵", "Enemy");
	public static string KcvBattleName => T("이름", "名前", "Name");
	public static string KcvBattleAttacker => T("공격", "攻撃", "Attacker");
	public static string KcvBattleTarget => T("대상", "対象", "Target");
	public static string KcvBattleKind => T("종류", "種別", "Type");
	public static string KcvBattleDamage => T("피해", "ダメージ", "Damage");
	public static string KcvBattleTargetHp => T("대상 HP", "対象 HP", "Target HP");
	public static string KcvBattleMiss => T("빗나감", "ミス", "Miss");
	public static string KcvBattleCritical => T("치명타", "クリティカル", "Critical");
	public static string KcvBattleSunk => T("격침", "撃沈", "Sunk");
	public static string KcvBattleHpBeforeAfter => T("HP 전 → 후", "HP 前 → 後", "HP before → after");
	public static string KcvBattleDamageTaken => T("받은 피해", "被ダメージ", "Damage taken");

	/// <summary>The battle could not be read again as the sortie record reads it, so the box shows the viewer's text.</summary>
	public static string KcvBattleDetailFallback => T(
		"이 전투는 표로 나누어 읽지 못해 74식 글 그대로 보여 줍니다.",
		"この戦闘は表に分けて読み込めなかったため、七四式の文章のまま表示します。",
		"This battle could not be split into tables, so it is shown as the viewer's text.");
}
