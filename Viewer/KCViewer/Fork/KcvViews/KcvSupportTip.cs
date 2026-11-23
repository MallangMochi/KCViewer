using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KCViewer.Data.Battle;
using KCViewer.Data.Battle.Detail;
using KCViewer.Data.Battle.Phase;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the support fleet's attack in the tooltip of the KanColleViewer battle panel's friendly fleet header (mockup
/// 20261123-support-fleet-v3, approved 2026-11-23 "구현"): the support's kind, the damage it dealt and that damage
/// against the enemy fleet's HP at the battle's start (floored to 0.1% as the panel's damage rates), then one line an
/// enemy ship: its number, name, damage (a critical hit marked) and HP before and after (a sinking marked), a miss
/// dimmed. No counts of hits, criticals or sinkings in the totals (the user's answer to the first mockup). The lines are
/// the viewer's own support details (PhaseSupport.BattleDetails), the ones its own tooltip writes as text.
/// </summary>
public sealed record KcvSupportTip(string Title, int TotalDamage, string RateText, IReadOnlyList<KcvSupportTipRow> Rows)
{
	public string TotalLabel => ForkStrings.KcvSupportTotalDamage;

	/// <summary>
	/// The support the panel shows (the viewer's choice in SetHPBar: a night support before a day battle, else the
	/// battle's own); null when no support came.
	/// </summary>
	public static KcvSupportTip? Of(BattleData battle, PhaseSupport? support)
	{
		if (support is not { IsAvailable: true }) return null;

		List<KcvSupportTipRow> rows = [];
		int total = 0;

		foreach (BattleDetail detail in support.BattleDetails)
		{
			BattleIndex index = detail.DefenderIndex;
			BattleDetail.CriticalType[] hits = detail.CriticalTypes
				.Where(c => c is not BattleDetail.CriticalType.Invalid)
				.ToArray();
			bool miss = hits.Length > 0 && hits.All(c => c is BattleDetail.CriticalType.Miss);
			bool critical = hits.Any(c => c is BattleDetail.CriticalType.Critical);
			int damage = detail.Damages.Sum();
			int before = Math.Max(detail.DefenderHP, 0);
			int after = Math.Max(before - damage, 0);

			total += damage;
			rows.Add(new(
				$"#{index.Index + 1 + (index.IsEscort ? 6 : 0)}",
				detail.Defender?.NameWithClass,
				miss ? ForkStrings.KcvBattleMiss : damage.ToString(CultureInfo.InvariantCulture),
				miss,
				critical ? " " + ForkStrings.KcvBattleCritical : null,
				before != after ? $"{before} → {after}" : null,
				before > 0 && after == 0 ? " " + ForkStrings.KcvBattleSunk : null));
		}

		int enemyHp = battle.Initial.EnemyInitialHPs.Where(hp => hp > 0).Sum()
			+ (battle.IsEnemyCombined && battle.Initial.EnemyInitialHPsEscort is { } escort ? escort.Where(hp => hp > 0).Sum() : 0);
		double rate = enemyHp > 0 ? (double)total / enemyHp : 0;

		string? kind = support.SupportFlag switch
		{
			1 => ForkStrings.KcvSupportAir,
			2 => ForkStrings.KcvSupportShelling,
			3 => ForkStrings.KcvSupportTorpedo,
			4 => ForkStrings.KcvSupportAntiSubmarine,
			_ => null,
		};

		// as BattleViewModel.HpRateDisplay: floored, so 99.99% does not read 100.0%
		string rateText = (Math.Floor(rate * 1000) / 1000).ToString("p1");

		return new(ForkStrings.KcvSupportTitle(kind), total, ForkStrings.KcvSupportEnemyRate(rateText), rows);
	}
}

/// <param name="Number">The enemy's number, #1 to #6 (#7 to #12 for an enemy escort fleet), as the viewer's text.</param>
/// <param name="Damage">The damage, or 빗나감 for a miss.</param>
/// <param name="Critical">「 치명타」 after the damage of a critical hit, in gold.</param>
/// <param name="Hp">HP before → after; none when it did not change.</param>
/// <param name="Sunk">「 격침」 after the HP when the support sank the ship, in gold; the name is dimmed.</param>
public sealed record KcvSupportTipRow(string Number, string? Name, string Damage, bool Miss, string? Critical, string? Hp, string? Sunk)
{
	public bool IsSunk => Sunk is not null;
}
