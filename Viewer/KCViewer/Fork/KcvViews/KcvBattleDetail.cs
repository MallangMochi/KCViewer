using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Common;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Attacks;
using KCViewer.Data;
using KCViewer.Data.Battle.Detail;
using KCViewer.Database.KancolleApi;
using KCViewer.Database.Sortie;
using KCViewer.KancolleApi.Types.ApiReqPractice.Battle;
using KCViewer.KancolleApi.Types.ApiReqPractice.MidnightBattle;
using KCViewer.KancolleApi.Types.Legacy.OpeningTorpedoRework;
using KCViewer.Services;
using KCViewer.Services.ApiFileService;
using KCViewer.Utility.Data;
using KCViewer.ViewModels;
using KCViewer.Window.Tools.SortieRecordViewer;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Node;
using KCViewer.Window.Tools.SortieRecordViewer.SortieDetail;
using LiveBattle = KCViewer.Data.Battle;

namespace KCViewer.Fork.KcvViews;

// Fork: the battle detail (the battle panel's 「전투 상세 표시」) and the sortie detail (the sortie record's 「출격 상세」) in
// KanColleViewer's style (mockup 20261010-battle-sortie-detail-v2, approved 2026-10-10 "1. 가 2. 넣음 3. 둠 4. 요약 없이
// 5. 구현"). Both read the battles as the sortie record does (Window/Tools/SortieRecordViewer/Sortie): the sortie detail
// its saved answers, the battle detail the answers of the battle in progress, which the viewer keeps (Data/Battle). The
// phases become folding boxes, all folded at first; the attacks a table, the fleets a table with equipment icons. While
// the style is off, and from the viewer's own windows, the viewer's text stays.

/// <summary>
/// The items of the boxes compare by reference, not by value as records do: a ship with the same equipment twice keeps
/// both icons as two items (ItemsControl, UI automation), the same for hits and rows.
/// </summary>
public abstract record KcvBattleItem
{
	public virtual bool Equals(KcvBattleItem? other) => ReferenceEquals(this, other);
	public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
}

/// <summary>An equipment icon of a ship in the 전력 table; the tooltip is the viewer's text of it 「[20] 영식 함상전투기 52형 &gt;&gt;」.</summary>
public sealed record KcvBattleIcon(EquipmentIconType Icon, string ToolTip) : KcvBattleItem;

public sealed class KcvBattleShipRow
{
	public required string Index { get; init; }
	public required string Type { get; init; }
	public required string Name { get; init; }
	public string? Id { get; init; }
	public required int Level { get; init; }
	public required int Hp { get; init; }
	public required int HpMax { get; init; }
	public required int Firepower { get; init; }
	public required int Torpedo { get; init; }
	public required int AntiAir { get; init; }
	public required int Armor { get; init; }
	public bool IsEnemy { get; init; }
	public bool IsEscaped { get; init; }
	public required IReadOnlyList<KcvBattleIcon> Equipment { get; init; }

	/// <summary>The expansion slot's equipment, after a line as in the fleet panel (none while it is empty or closed).</summary>
	public KcvBattleIcon? Expansion { get; init; }

	public bool IsAlternate { get; init; }
	public SolidColorBrush HpBrush => KcvBrushes.Gauge(HpMax > 0 ? (double)Hp / HpMax : 0);
}

/// <summary>A fleet of the 전력 box: its name, the viewer's header (air power and LoS, or the enemy's air power), its ships.</summary>
public sealed class KcvBattleFleetBlock
{
	public required string Title { get; init; }
	public string? Summary { get; init; }
	public bool IsEnemy { get; init; }
	public required IReadOnlyList<KcvBattleShipRow> Ships { get; init; }
}

/// <summary>The viewer's text where the mockup has no table (support fleet, friendly fleet, night gear, air bases).</summary>
public sealed record KcvBattleTextBlock(string? Title, string Text) : KcvBattleItem;

public enum KcvBattleTone
{
	Neutral,
	Good,
	Bad,
}

public sealed record KcvBattleSegment(string Text, bool IsStrong) : KcvBattleItem;

/// <summary>A row 「Stage 1 [제공권 확보] 아군 -1 / 82 · 적 -0 / 0」 or 「진형 아군 단종진 · 적 복종진」.</summary>
public sealed record KcvBattleInfoRow(string Label, string? Chip, KcvBattleTone ChipTone, IReadOnlyList<KcvBattleSegment> Segments) : KcvBattleItem;

public sealed record KcvBattleInfoBlock(IReadOnlyList<KcvBattleInfoRow> Rows) : KcvBattleItem;

/// <summary>A hit of an attack: its damage (or 「빗나감」), critical, protected (かばう); HasNext puts a comma after it.</summary>
public sealed record KcvBattleHit(string Damage, bool IsCritical, bool IsMiss, bool IsProtected, bool HasNext) : KcvBattleItem;

public sealed class KcvBattleAttackRow
{
	public required string Attacker { get; init; }
	public bool AttackerIsEnemy { get; init; }
	public required string Target { get; init; }
	public bool TargetIsEnemy { get; init; }
	public required string Kind { get; init; }

	/// <summary>A cut-in or a special attack, shown as a chip.</summary>
	public bool IsSpecial { get; init; }

	public required IReadOnlyList<KcvBattleHit> Hits { get; init; }

	/// <summary>The target's HP before and after, only when it changed (as the viewer writes 「(80 → 71)」).</summary>
	public int? HpBefore { get; init; }
	public int? HpAfter { get; init; }
	public bool IsSunk { get; init; }

	/// <summary>The viewer's 「응급 수리 요원 발동 HP7」 when a repair goddess or personnel saved the target.</summary>
	public string? Damecon { get; init; }

	public bool IsAlternate { get; set; }
}

public sealed record KcvBattleAttackBlock(IReadOnlyList<KcvBattleAttackRow> Rows) : KcvBattleItem;

public sealed class KcvBattleEndRow
{
	public required string Name { get; init; }
	public required int Before { get; init; }
	public required int After { get; init; }
	public required int Max { get; init; }

	/// <summary>All the damage taken, overkill too (the viewer's number in brackets 「(48 → 0)/48 (-311)」, as a positive one).</summary>
	public required int Taken { get; init; }

	public bool IsEnemy { get; init; }
	public bool IsAlternate { get; init; }
	public bool HasTaken => Taken > 0;
	public SolidColorBrush HpBrush => KcvBrushes.Gauge(Max > 0 ? (double)After / Max : 0);
}

public sealed record KcvBattleEndTable(string Title, bool IsEnemy, IReadOnlyList<KcvBattleEndRow> Rows) : KcvBattleItem;

/// <summary>전투 종료: the friendly fleets (or the air bases of an air raid) on the left, the enemy's on the right.</summary>
public sealed record KcvBattleEndBlock(IReadOnlyList<KcvBattleEndTable> Friend, IReadOnlyList<KcvBattleEndTable> Enemy) : KcvBattleItem;

public sealed record KcvBattleResultRow(string Label, string Value, bool IsDim) : KcvBattleItem;

public sealed record KcvBattleResultBlock(string Rank, IReadOnlyList<KcvBattleResultRow> Rows) : KcvBattleItem;

/// <summary>A folding box: a phase, 전투 종료 or 전투 결과. All boxes are folded at first (the user's answer 5).</summary>
public sealed class KcvBattleSection(string title, IReadOnlyList<object> blocks) : ObservableObject
{
	public string Title { get; } = title;
	public IReadOnlyList<object> Blocks { get; } = blocks;

	private bool isExpanded;
	public bool IsExpanded
	{
		get => isExpanded;
		set => SetProperty(ref isExpanded, value);
	}
}

/// <summary>The name of a battle before its boxes, when a cell has more than one (an air raid and a battle, day and night).</summary>
public sealed record KcvBattleHeading(string Title) : KcvBattleItem;

/// <summary>The boxes of a cell from the sortie record's battles.</summary>
public static class KcvBattleDetailBuilder
{
	private static string Friend => ForkStrings.KcvBattleFriend;
	private static string Enemy => ForkStrings.KcvBattleEnemy;

	/// <summary>The viewer's names without their brackets: 「〈아군 함대〉」 → 「아군 함대」, 「◆ 전투 결과 ◆」 → 「전투 결과」.</summary>
	public static string Plain(string text) => text.Trim().Trim('〈', '〉', '◆', '《', '》').Trim();

	/// <summary>The label of the viewer's 「제독 경험치: +{0}」.</summary>
	private static string Label(string format) => format.Split(':')[0].Trim();

	/// <summary>The air raid, the battles (with their names when there are more than one) and the result of a cell.</summary>
	public static List<object> Items(SortieNode node, KcvBattleResultBlock? result = null)
	{
		List<BattleData> battles = [];
		if (node.AirBaseRaid is { } raid) battles.Add(raid);

		if (node is BattleNode battleNode)
		{
			battles.Add(battleNode.FirstBattle);
			if (battleNode.SecondBattle is { } second) battles.Add(second);
		}

		List<object> items = [];
		foreach (BattleData battle in battles)
		{
			if (battles.Count > 1) items.Add(new KcvBattleHeading(battle.Title));

			items.AddRange(Battle(battle));
		}

		result ??= node is BattleNode { BattleResult: not null } withResult ? Result(withResult) : null;
		if (result is not null) items.Add(new KcvBattleSection(Plain(ConstantsRes.BattleDetail_Result), [result]));

		return items;
	}

	public static IEnumerable<KcvBattleSection> Sections(IEnumerable<object> items) => items.OfType<KcvBattleSection>();

	private static IEnumerable<KcvBattleSection> Battle(BattleData battle)
	{
		foreach (PhaseBase phase in battle.Phases)
		{
			if (Phase(phase) is { } section) yield return section;
		}

		yield return new KcvBattleSection(Plain(ConstantsRes.BattleDetail_BattleEnd), [End(battle)]);
	}

	private static KcvBattleSection? Phase(PhaseBase phase) => phase switch
	{
		PhaseInitial p => new(p.Title, Initial(p)),
		PhaseSearching p => new(ForkStrings.KcvBattleSearching, [Searching(p)]),
		PhaseBaseAirRaid p => new(p.Title, Blocks(
			Text(p.Display),
			AirInfo(p.AirState, p.Stage1Display is not null, p.Stage1FLostcount, p.Stage1FCount, p.Stage1ELostcount, p.Stage1ECount,
				p.TouchAircraftFriend, p.TouchAircraftEnemy,
				p.Stage2Display, p.Stage2FLostcount, p.Stage2FCount, p.Stage2ELostcount, p.Stage2ECount),
			Attacks(p.AttackDisplays.Select(Row)))),
		PhaseBaseAirAttack p => new(p.Title, p.Units.SelectMany(Unit).ToList()),
		PhaseJetBaseAirAttack p => new(p.Title, p.Units.SelectMany(Unit).ToList()),
		PhaseAirBattleBase p => new(p.Title, Blocks(AirInfo(p), Attacks(p.AttackDisplays.Select(Row)))),
		PhaseSupport p => new(p.Title, Blocks(Text(p.SupportFleetDisplay), Attacks(p.AttackDisplays.Select(Row)))),
		PhaseFriendlySupportInfo p => new(p.Title, Blocks(Text(p.Display))),
		PhaseFriendlyShelling p => new(p.Title, Blocks(Text(p.InitialDisplay), Attacks(p.AttackDisplays.Select(Row)))),
		PhaseNightInitial p => new(p.Title, Blocks(Text(p.Display))),
		PhaseShelling p => new(p.Title, Blocks(Attacks(p.AttackDisplays.Select(Row)))),
		PhaseTorpedo p => new(p.Title, Blocks(Attacks(p.AttackDisplays.Select(Row)))),
		PhaseNightBattle p => new(p.Title, Blocks(Attacks(p.AttackDisplays.Select(Row)))),
		_ => null,
	};

	private static IReadOnlyList<object> Blocks(params object?[] blocks) => blocks.OfType<object>().ToList();

	private static IEnumerable<object> Unit(PhaseBaseAirAttackUnit unit) => Blocks(
		Text(unit.Display),
		AirInfo(unit),
		Attacks(unit.AttackDisplays.Select(Row)));

	private static KcvBattleTextBlock? Text(string? text) => string.IsNullOrWhiteSpace(text)
		? null
		: new KcvBattleTextBlock(null, text.Replace("\r", "").Trim('\n'));

	/// <summary>The viewer's text whose first line is a name in brackets 「〈기지 항공대〉」: that line becomes the title.</summary>
	private static KcvBattleTextBlock? TitledText(string? text)
	{
		if (string.IsNullOrWhiteSpace(text)) return null;

		string[] lines = text.Replace("\r", "").Trim('\n').Split('\n');

		// 「〈기지 항공대〉」 alone (no air base in the fleets the viewer kept) is a title without lines
		return new KcvBattleTextBlock(Plain(lines[0]), string.Join("\n", lines.Skip(1)));
	}

	// 전력

	private static IReadOnlyList<object> Initial(PhaseInitial initial)
	{
		List<object> blocks = [];
		if (initial.FleetsAfterPhase is not { } fleets) return blocks;

		if (initial.IsAirBaseRaid)
		{
			if (TitledText(initial.AirRaidAirBaseDisplay) is { } bases) blocks.Add(bases);
		}
		else
		{
			string title = fleets.EscortFleet is null ? ConstantsRes.BattleDetail_FriendFleet : ConstantsRes.BattleDetail_FriendMainFleet;
			blocks.Add(FriendFleet(fleets.Fleet, title, false));

			if (fleets.EscortFleet is { } escort)
			{
				blocks.Add(FriendFleet(escort, ConstantsRes.BattleDetail_FriendEscortFleet, true));
			}
		}

		bool bossDamaged = initial.ApiXal01 > 0;

		if (fleets.EnemyFleet is { } enemy)
		{
			string title = fleets.EnemyEscortFleet is null ? ConstantsRes.BattleDetail_EnemyFleet : ConstantsRes.BattleDetail_EnemyMainFleet;
			blocks.Add(EnemyFleet(enemy, title, false, bossDamaged));
		}

		if (fleets.EnemyEscortFleet is { } enemyEscort)
		{
			blocks.Add(EnemyFleet(enemyEscort, ConstantsRes.BattleDetail_EnemyEscortFleet, true, bossDamaged));
		}

		if (initial.HasAirBaseAttack && TitledText(initial.AirBaseDisplay) is { } airBases) blocks.Add(airBases);

		return blocks;
	}

	private static KcvBattleFleetBlock FriendFleet(IFleetData fleet, string title, bool isEscort) => new()
	{
		Title = Plain(title),
		Summary = $"{BattleRes.AirSuperiority} {Calculator.GetAirSuperiorityRangeString(fleet)} · {Los(fleet)}",
		Ships = Ships(fleet, isEscort, false),
	};

	private static KcvBattleFleetBlock EnemyFleet(IFleetData fleet, string title, bool isEscort, bool bossDamaged) => new()
	{
		Title = Plain(title),
		Summary = string.Format(BattleRes.AirBaseAirPower, Calculator.GetAirSuperiority(fleet), PhaseInitial.AirBaseAirPower(fleet)).Trim()
			+ (bossDamaged ? BattleRes.BossDebuffed : ""),
		IsEnemy = true,
		Ships = Ships(fleet, isEscort, true),
	};

	/// <summary>「색적 능력 [1] 14.78 [2] 26.98 [3] 39.18 [4] 51.38」 from the viewer's 「 / 색적 능력 [1] {0}, [2] {1}, …」.</summary>
	private static string Los(IFleetData fleet)
	{
		double[] los = Enumerable.Range(1, 4)
			.Select(w => Math.Round(Calculator.GetSearchingAbility_New33(fleet, w), 2, MidpointRounding.ToNegativeInfinity))
			.ToArray();

		return string.Format(BattleRes.Los, los[0], los[1], los[2], los[3]).Trim().TrimStart('/').Trim().Replace(", ", " ");
	}

	private static IReadOnlyList<KcvBattleShipRow> Ships(IFleetData fleet, bool isEscort, bool isEnemy) => fleet.MembersInstance
		.Select((ship, i) => (Ship: ship, Index: i))
		.Where(t => t.Ship is not null)
		.Select((t, row) => new KcvBattleShipRow
		{
			Index = (t.Index + (isEscort ? 7 : 1)).ToString(),
			Type = t.Ship!.MasterShip.ShipTypeName,
			Name = t.Ship.Name,
			Id = isEnemy ? ((int)t.Ship.MasterShip.ShipID).ToString() : null,
			Level = t.Ship.Level,
			Hp = t.Ship.HPCurrent,
			HpMax = t.Ship.HPMax,
			Firepower = t.Ship.FirepowerBase,
			Torpedo = t.Ship.TorpedoBase,
			AntiAir = t.Ship.AABase,
			Armor = t.Ship.ArmorBase,
			IsEnemy = isEnemy,
			IsEscaped = !isEnemy && fleet.EscapedShipList.Contains(t.Index),
			Equipment = t.Ship.SlotInstance
				.Select((eq, slot) => (Equipment: eq, Slot: slot))
				.Where(s => s.Equipment is not null)
				.Select(s => Icon(s.Equipment!, s.Slot < t.Ship.Aircraft.Count ? t.Ship.Aircraft[s.Slot] : 0))
				.ToList(),
			Expansion = t.Ship.ExpansionSlotInstance is { } expansion ? Icon(expansion, 0) : null,
			IsAlternate = row % 2 == 1,
		})
		.ToList();

	// the viewer's text of a slot (PhaseInitial): aircraft with their count, the rest with their level and proficiency
	private static KcvBattleIcon Icon(IEquipmentData equipment, int aircraft) => new(
		equipment.MasterEquipment.IconTypeTyped,
		equipment.MasterEquipment.IsAircraft ? $"[{aircraft}] {equipment.NameWithLevel}" : equipment.NameWithLevel);

	// 색적·진형

	private static KcvBattleInfoBlock Searching(PhaseSearching searching)
	{
		List<KcvBattleInfoRow> rows =
		[
			Info(ForkStrings.KcvBattleFormation,
				$"{Friend} {Constants.GetFormation(searching.PlayerFormationType)} · {Enemy} {Constants.GetFormation(searching.EnemyFormationType)}"),
			Info(BattleRes.Engagement, Constants.GetEngagementForm(searching.EngagementType)),
			Info(BattleRes.PhaseSearching,
				$"{Friend} {Constants.GetSearchingResult(searching.PlayerDetectionType)} · {Enemy} {Constants.GetSearchingResult(searching.EnemyDetectionType)}"),
		];

		if (searching.SmokeCount > 0) rows.Add(Info(BattleRes.SmokeScreen, $"x{searching.SmokeCount}"));

		return new(rows);
	}

	private static KcvBattleInfoRow Info(string label, string text) => new(label, null, KcvBattleTone.Neutral, [new(text, false)]);

	// 항공전

	private static KcvBattleInfoBlock AirInfo(PhaseAirBattleBase air) => AirInfo(
		air.AirState, air.Stage1Display is not null, air.Stage1FLostcount, air.Stage1FCount, air.Stage1ELostcount, air.Stage1ECount,
		air.TouchAircraftFriend, air.TouchAircraftEnemy,
		air.Stage2Display, air.Stage2FLostcount, air.Stage2FCount, air.Stage2ELostcount, air.Stage2ECount);

	private static KcvBattleInfoBlock AirInfo(AirState state, bool hasStage1, int friendLost1, int friend1, int enemyLost1, int enemy1,
		string? touchFriend, string? touchEnemy,
		string? stage2Display, int friendLost2, int friend2, int enemyLost2, int enemy2)
	{
		List<KcvBattleInfoRow> rows = [];

		if (hasStage1)
		{
			List<KcvBattleSegment> segments = Counts(friendLost1, friend1, enemyLost1, enemy1);
			if (touchFriend is not null) segments.Add(new($" · {BattleRes.Contact} {touchFriend}", false));
			if (touchEnemy is not null) segments.Add(new($" · {BattleRes.EnemyContact} {touchEnemy}", false));

			string? chip = state is AirState.Unknown ? null : Constants.GetAirSuperiority(state);
			rows.Add(new("Stage 1", chip, Tone(state), segments));
		}

		if (stage2Display is not null)
		{
			List<KcvBattleSegment> segments = Counts(friendLost2, friend2, enemyLost2, enemy2);

			// the viewer's 「Stage 2:」 line ends with the anti-air cut-in 「대공 컷인 5 ( 아키즈키 改 Lv. 99, … )」
			const string stage2 = "Stage 2:";
			string first = stage2Display.Replace("\r", "").Split('\n')[0];
			string cutIn = first.StartsWith(stage2, StringComparison.Ordinal) ? first[stage2.Length..].Trim() : "";
			if (cutIn.Length > 0) segments.Add(new($" · {cutIn}", false));

			rows.Add(new("Stage 2", null, KcvBattleTone.Neutral, segments));
		}

		return new(rows);
	}

	private static List<KcvBattleSegment> Counts(int friendLost, int friend, int enemyLost, int enemy) =>
	[
		new($"{Friend} ", false),
		new($"-{friendLost} / {friend}", true),
		new($" · {Enemy} ", false),
		new($"-{enemyLost} / {enemy}", true),
	];

	// air supremacy and superiority in green, denial and incapability in red, parity as it is
	private static KcvBattleTone Tone(AirState state) => state switch
	{
		AirState.Supremacy or AirState.Superiority => KcvBattleTone.Good,
		AirState.Denial or AirState.Incapability => KcvBattleTone.Bad,
		_ => KcvBattleTone.Neutral,
	};

	// attacks

	private static KcvBattleAttackBlock? Attacks(IEnumerable<KcvBattleAttackRow> rows)
	{
		List<KcvBattleAttackRow> list = rows.ToList();
		if (list.Count == 0) return null;

		for (int i = 0; i < list.Count; i++) list[i].IsAlternate = i % 2 == 1;

		return new(list);
	}

	private static string ShipName(IShipData ship, BattleIndex index) => $"{ship.Name} {index.Display}";

	// cut-ins and special attacks: the kinds before the plain ones (shelling, air attack, depth charge, torpedo = 1000…)
	private static bool IsSpecial(int kind) => kind is > 0 and < 1000;

	private static KcvBattleAttackRow Row(PhaseShellingAttackViewModel attack)
	{
		DayAttackKind kind = attack.Attacks.FirstOrDefault()?.AttackKind ?? DayAttackKind.Unknown;

		return Row(ShipName(attack.Attacker, attack.AttackerIndex), attack.AttackerIndex.FleetFlag is FleetFlag.Enemy,
			ShipName(attack.Defender, attack.DefenderIndex), attack.DefenderIndex.FleetFlag is FleetFlag.Enemy,
			Core.Types.Attacks.DayAttack.AttackDisplay(kind, attack.DisplayEquipment), IsSpecial((int)kind),
			attack.Attacks.Select(a => ((double)a.Damage, a.CriticalFlag, a.GuardsFlagship)),
			attack.Defender, attack.DefenderHpBeforeAttacks[0]);
	}

	private static KcvBattleAttackRow Row(PhaseTorpedoAttackViewModel attack)
	{
		DayAttackKind kind = attack.Attacks.FirstOrDefault()?.AttackKind ?? DayAttackKind.Unknown;

		return Row(ShipName(attack.Attacker, attack.AttackerIndex), attack.AttackerIndex.FleetFlag is FleetFlag.Enemy,
			ShipName(attack.Defender, attack.DefenderIndex), attack.DefenderIndex.FleetFlag is FleetFlag.Enemy,
			Core.Types.Attacks.DayAttack.AttackDisplay(kind), IsSpecial((int)kind),
			attack.Attacks.Select(a => ((double)a.Damage, a.CriticalFlag, a.GuardsFlagship)),
			attack.Defender, attack.DefenderHpBeforeAttacks[0]);
	}

	private static KcvBattleAttackRow Row(PhaseNightBattleAttackViewModel attack)
	{
		NightAttackKind kind = attack.Attacks.FirstOrDefault()?.AttackKind ?? NightAttackKind.Unknown;

		return Row(ShipName(attack.Attacker, attack.AttackerIndex), attack.AttackerIndex.FleetFlag is FleetFlag.Enemy,
			ShipName(attack.Defender, attack.DefenderIndex), attack.DefenderIndex.FleetFlag is FleetFlag.Enemy,
			Core.Types.Attacks.NightAttack.AttackDisplay(kind, attack.DisplayEquipment), IsSpecial((int)kind),
			attack.Attacks.Select(a => ((double)a.Damage, a.CriticalFlag, a.GuardsFlagship)),
			attack.Defender, attack.DefenderHpBeforeAttacks[0]);
	}

	private static KcvBattleAttackRow Row(PhaseFriendNightBattleAttackViewModel attack)
	{
		NightAttackKind kind = attack.Attacks.FirstOrDefault()?.AttackKind ?? NightAttackKind.Unknown;

		return Row(ShipName(attack.Attacker, attack.AttackerIndex), attack.AttackerIndex.FleetFlag is FleetFlag.Enemy,
			ShipName(attack.Defender, attack.DefenderIndex), attack.DefenderIndex.FleetFlag is FleetFlag.Enemy,
			Core.Types.Attacks.NightAttack.AttackDisplay(kind, attack.DisplayEquipment), IsSpecial((int)kind),
			attack.Attacks.Select(a => ((double)a.Damage, a.CriticalFlag, a.GuardsFlagship)),
			attack.Defender, attack.DefenderHpBeforeAttacks[0]);
	}

	private static KcvBattleAttackRow Row(PhaseSupportAttackViewModel attack) => Row(
		attack.Attacker, attack.DefenderIndex.FleetFlag is FleetFlag.Player,
		ShipName(attack.Defender, attack.DefenderIndex), attack.DefenderIndex.FleetFlag is FleetFlag.Enemy,
		attack.AttackKind, false,
		attack.Attacks.Select(a => ((double)a.Damage, a.CriticalFlag, a.GuardsFlagship)),
		attack.Defender, attack.DefenderHpBeforeAttacks[0]);

	private static KcvBattleAttackRow Row(AirBattleAttackViewModel attack) => Row(
		attack.AttackerName, attack.DefenderIndex.FleetFlag is FleetFlag.Player,
		ShipName(attack.Defender, attack.DefenderIndex), attack.DefenderIndex.FleetFlag is FleetFlag.Enemy,
		attack.AttackKind, false,
		[(attack.Damage, attack.HitType, attack.GuardsFlagship)],
		attack.Defender, attack.DefenderHpBeforeAttack);

	private static KcvBattleAttackRow Row(AirBaseRaidAttackViewModel attack) => Row(
		attack.AttackerName, attack.DefenderIndex.FleetFlag is FleetFlag.Player,
		$"{attack.Defender.Name} {attack.DefenderIndex.Display}", false,
		attack.AttackKind, false,
		[(attack.Damage, attack.HitType, attack.GuardsFlagship)],
		null, attack.DefenderHpBeforeAttack);

	/// <summary>
	/// A row as the viewer's text 「[컷인(주포/주포)] 202 Dmg (80 → 0)」: the HP only when it changed, the damage control
	/// when the target would sink with one (the viewer's attack view models).
	/// </summary>
	private static KcvBattleAttackRow Row(string attacker, bool attackerIsEnemy, string target, bool targetIsEnemy,
		string kind, bool isSpecial, IEnumerable<(double Damage, HitType Hit, bool Guards)> hits, IShipData? defender, int hpBefore)
	{
		List<(double Damage, HitType Hit, bool Guards)> list = hits.ToList();
		int hpAfter = Math.Max(0, hpBefore - list.Sum(h => (int)h.Damage));

		string? damecon = null;
		if (defender is not null && hpAfter <= 0
			&& defender.AllSlotInstance.FirstOrDefault(e => e?.MasterEquipment.CategoryType is EquipmentTypes.DamageControl) is { } repair)
		{
			damecon = repair.EquipmentId switch
			{
				EquipmentId.DamageControl_EmergencyRepairGoddess => $"{BattleRes.GoddessActivated} HP{defender.HPMax}",
				EquipmentId.DamageControl_EmergencyRepairPersonnel => $"{BattleRes.DameconActivated} HP{(int)(defender.HPMax * 0.2)}",
				_ => null,
			};
		}

		bool showHp = hpBefore > 0 && hpBefore != hpAfter;

		return new KcvBattleAttackRow
		{
			Attacker = attacker,
			AttackerIsEnemy = attackerIsEnemy,
			Target = target,
			TargetIsEnemy = targetIsEnemy,
			Kind = kind,
			IsSpecial = isSpecial,
			Hits = list
				.Select((h, i) => new KcvBattleHit(
					h.Hit is HitType.Miss ? ForkStrings.KcvBattleMiss : h.Damage.ToString(),
					h.Hit is HitType.Critical, h.Hit is HitType.Miss, h.Guards, i < list.Count - 1))
				.ToList(),
			HpBefore = showHp ? hpBefore : null,
			HpAfter = showHp ? hpAfter : null,
			IsSunk = showHp && hpAfter == 0 && damecon is null && defender is not null,
			Damecon = damecon,
		};
	}

	// 전투 종료

	private static KcvBattleEndBlock End(BattleData battle)
	{
		List<KcvBattleEndTable> friend = [];

		if (battle.Initial.IsAirBaseRaid)
		{
			List<KcvBattleEndRow> bases = battle.AirBaseBeforeAfter
				.Where(b => b is { Before: not null, After: not null })
				.Select((b, i) => EndRow(b.Before!.Name, b.Before.HPCurrent, b.After!.HPCurrent, b.Before.HPMax, false, i))
				.ToList();
			friend.Add(new(Plain(ConstantsRes.BattleDetail_FriendFleet), false, bases));
		}
		else
		{
			string title = battle.EscortFleetBeforeAfter is null ? ConstantsRes.BattleDetail_FriendFleet : ConstantsRes.BattleDetail_FriendMainFleet;
			friend.Add(new(Plain(title), false, EndRows(battle.MainFleetBeforeAfter, false)));

			if (battle.EscortFleetBeforeAfter is { } escort)
			{
				friend.Add(new(Plain(ConstantsRes.BattleDetail_FriendEscortFleet), false, EndRows(escort, false)));
			}
		}

		List<KcvBattleEndTable> enemy = [];

		if (battle.EnemyMainFleetBeforeAfter is { } enemyMain)
		{
			string title = battle.EnemyEscortFleetBeforeAfter is null ? ConstantsRes.BattleDetail_EnemyFleet : ConstantsRes.BattleDetail_EnemyMainFleet;
			enemy.Add(new(Plain(title), true, EndRows(enemyMain, true)));
		}

		if (battle.EnemyEscortFleetBeforeAfter is { } enemyEscort)
		{
			enemy.Add(new(Plain(ConstantsRes.BattleDetail_EnemyEscortFleet), true, EndRows(enemyEscort, true)));
		}

		return new(friend, enemy);
	}

	private static List<KcvBattleEndRow> EndRows(IEnumerable<ShipBeforeAfter> ships, bool isEnemy) => ships
		.Where(s => s is { Before: not null, After: not null })
		.Select((s, i) => EndRow(s.Before!.Name, s.Before.HPCurrent, s.After!.HPCurrent, s.Before.HPMax, isEnemy, i))
		.ToList();

	private static KcvBattleEndRow EndRow(string name, int before, int after, int max, bool isEnemy, int row) => new()
	{
		Name = name,
		Before = before,
		After = Math.Max(0, after),
		Max = max,
		Taken = before - after,
		IsEnemy = isEnemy,
		IsAlternate = row % 2 == 1,
	};

	// 전투 결과

	public static KcvBattleResultBlock Result(BattleNode node)
	{
		BattleResult result = node.BattleResult!;
		BattleFleets fleets = node.FirstBattle.FleetsBeforeBattle;

		return Result(result.WinRank,
			fleets.EscortFleet is null ? null : Mvp(fleets.Fleet, result.MvpIndex),
			fleets.EscortFleet is null ? Mvp(fleets.Fleet, result.MvpIndex) : null,
			fleets.EscortFleet is null ? null : Mvp(fleets.EscortFleet, result.MvpIndexCombined ?? -1),
			result.AdmiralExp, result.BaseExp,
			Drops(result.DroppedShipId is { } ship ? (int)ship : 0, result.DroppedEquipmentId ?? 0, result.DroppedItemId ?? 0));
	}

	/// <summary>A practice battle, whose result the sortie record does not read: from the viewer's result (Data/Battle).</summary>
	public static KcvBattleResultBlock Result(LiveBattle.BattleManager bm)
	{
		LiveBattle.BattleResultData result = bm.Result;
		LiveBattle.Phase.PhaseInitial initial = bm.FirstBattle.Initial;

		string? Name(IFleetData? fleet, int index) => index > 0 && fleet is not null && index <= fleet.MembersInstance.Count
			? fleet.MembersInstance[index - 1]?.NameWithLevel
			: null;

		return Result(result.Rank,
			bm.IsCombinedBattle ? Name(initial.FriendFleet, result.MVPIndex) : null,
			bm.IsCombinedBattle ? null : Name(initial.FriendFleet, result.MVPIndex),
			bm.IsCombinedBattle ? Name(initial.FriendFleetEscort, result.MVPIndexCombined) : null,
			result.AdmiralExp, result.BaseExp,
			bm.IsPractice ? null : Drops(result.DroppedShipID, result.DroppedEquipmentID, result.DroppedItemID));
	}

	private static string? Mvp(IFleetData fleet, int index) => index >= 0 && index < fleet.MembersInstance.Count
		? fleet.MembersInstance[index]?.NameWithLevel
		: null;

	private static KcvBattleResultBlock Result(string rank, string? mvpMain, string? mvp, string? mvpEscort,
		int admiralExp, int baseExp, List<string>? drops)
	{
		List<KcvBattleResultRow> rows = [];

		if (mvpEscort is null && mvpMain is null)
		{
			rows.Add(new("MVP", mvp ?? ConstantsRes.NoNode, mvp is null));
		}
		else
		{
			rows.Add(new(Label(ConstantsRes.BattleDetail_ResultMVPMain), mvpMain ?? ConstantsRes.NoNode, mvpMain is null));
			rows.Add(new(Label(ConstantsRes.BattleDetail_ResultMVPEscort), mvpEscort ?? ConstantsRes.NoNode, mvpEscort is null));
		}

		rows.Add(new(Label(ConstantsRes.BattleDetail_AdmiralExp), $"+{admiralExp}", false));
		rows.Add(new(Label(ConstantsRes.BattleDetail_ShipExp), $"+{baseExp}", false));

		if (drops is not null)
		{
			rows.Add(new(Label(ConstantsRes.BattleDetail_Drop), drops.Count > 0 ? string.Join("\n", drops) : ConstantsRes.NoNode, drops.Count == 0));
		}

		return new(rank, rows);
	}

	// the viewer's drops: a ship 「구축함 사츠키」, an equipment, an item
	private static List<string> Drops(int shipId, int equipmentId, int itemId)
	{
		KCDatabase db = KCDatabase.Instance;
		List<string> drops = [];

		if (db.MasterShips[shipId] is { } ship) drops.Add($"{ship.ShipTypeName} {ship.NameWithClass}");
		if (db.MasterEquipments[equipmentId] is { } equipment) drops.Add($"{equipment.CategoryTypeInstance.NameEN} {equipment.NameEN}");
		if (db.MasterUseItems[itemId] is { } item) drops.Add(item.NameTranslated);

		return drops;
	}
}

/// <summary>The battle in progress read again as the sortie record reads its battles (the answers the viewer keeps).</summary>
public static class KcvLiveBattle
{
	/// <summary>The cell of the battle in progress; null when the sortie record cannot read it (heavy air raids, night to day).</summary>
	public static SortieNode? Node(LiveBattle.BattleManager bm)
	{
		if (bm.HeavyBaseAirRaids.Count > 0) return null;

		KCDatabase db = KCDatabase.Instance;
		int fleetId = bm.BattleDay?.Initial.FriendFleetID ?? bm.BattleNight?.Initial.FriendFleetID ?? 1;

		if (bm.IsPractice)
		{
			if (bm.BattleDay is not { } practiceDay) return null;

			PhaseFactory phases = Ioc.Default.GetRequiredService<PhaseFactory>();
			BattleFleets practiceFleets = Fleets(fleetId, 0, FleetType.Single, 0, 0);

			BattleData first = Read(practiceDay.APIName, (object)practiceDay.RawData) switch
			{
				ApiReqPracticeBattleResponse day => new BattlePracticeDay(phases, practiceFleets, day),
				OpeningTorpedoRework_ApiReqPracticeBattleResponse day => new BattlePracticeDay(phases, practiceFleets, day),
				_ => throw new NotSupportedException(practiceDay.APIName),
			};

			BattleNode practice = new(db, 0, 0, 0, first, CellType.Unknown, 0, 0);

			if (bm.BattleNight is { } night && Read(night.APIName, (object)night.RawData) is ApiReqPracticeMidnightBattleResponse midnight)
			{
				practice.SecondBattle = new BattlePracticeNight(phases, first.FleetsAfterBattle, midnight);
			}

			return practice;
		}

		if (bm.Compass is not { ForkApiName: { } compassApi } compass) return null;

		int world = compass.MapAreaID;
		BattleFleets fleets = Fleets(fleetId, world, db.Fleet.CombinedFlag,
			db.Fleet.NodeSupportFleetId(world) ?? 0, db.Fleet.BossSupportFleetId(world) ?? 0);

		SortieRecord sortie = new()
		{
			Version = 1,
			World = world,
			Map = compass.MapInfoID,
			FleetData = ApiFileService.MakeSortieFleet([], [], FleetType.Single, fleetId, 0, 0, world),
			MapData = new(),
		};

		// the database is only for the menu's commands, which this window does not have
		SortieDetailViewModel detail = new(null!, sortie, fleets, null);
		DateTime time = DateTime.UtcNow;

		detail.AddApiFile(Read(compassApi, (object)compass.RawData) ?? throw new NotSupportedException(compassApi), time);

		foreach (LiveBattle.BattleData? battle in new[] { bm.FirstBattle, bm.SecondBattle })
		{
			// an air raid comes with the cell's answer (api_destruction_battle), which the sortie record reads itself
			if (battle is null or LiveBattle.BattleBaseAirRaid) continue;

			detail.AddApiFile(Read(battle.APIName, (object)battle.RawData) ?? throw new NotSupportedException(battle.APIName), time);
		}

		if (bm.Result is { } result)
		{
			detail.AddApiFile(Read(result.ForkApiName, (object)result.RawData) ?? throw new NotSupportedException(result.ForkApiName), time);
		}

		detail.EnsureApiFilesProcessed();

		return detail.Nodes.LastOrDefault();
	}

	// the fleets as the sortie record keeps them at a sortie (ApiFileService), as ship mocks the battles can change
	private static BattleFleets Fleets(int fleetId, int world, FleetType combinedFlag, int nodeSupportFleetId, int bossSupportFleetId)
	{
		KCDatabase db = KCDatabase.Instance;

		SortieFleetData data = ApiFileService.MakeSortieFleet(db.Fleet.Fleets.Values, db.BaseAirCorps.Values, combinedFlag,
			fleetId, nodeSupportFleetId, bossSupportFleetId, world);

		return ToolService.MakeBattleFleets(data, fleetId);
	}

	// an answer the viewer keeps (api_data, DynaJson) as the typed answer of the sortie record, as the viewer does itself
	// (Data/PoiDbSubmission: JsonSerializer.Deserialize(data.ToString()))
	private static object? Read(string apiName, object rawData) => new ApiFile
	{
		ApiFileType = ApiFileType.Response,
		Name = apiName,
		Content = $"{{\"api_result\":1,\"api_result_msg\":\"\",\"api_data\":{rawData}}}",
		TimeStamp = DateTime.UtcNow,
		Version = 1,
	}.GetResponseApiData();

	/// <summary>
	/// 「연습」 or 「진수부 정면 해역 (1-1) [甲]」, as the viewer's battle detail begins (BattleDetailDescriptor).
	/// </summary>
	public static string MapText(LiveBattle.BattleManager bm)
	{
		if (bm.IsPractice || bm.Compass is not { } compass) return BattleRes.Exercise;

		string text = $"{compass.MapInfo.NameEN} ({compass.MapAreaID}-{compass.MapInfoID})";
		if (compass.MapInfo.EventDifficulty > 0) text += $" [{Constants.GetDifficulty(compass.MapInfo.EventDifficulty)}]";

		return text;
	}

	/// <summary>The gauge of a map not cleared yet, as the viewer's battle detail writes it 「#2 1200 / 2000 HP」.</summary>
	public static string? Gauge(LiveBattle.BattleManager bm)
	{
		if (bm.IsPractice || bm.Compass is not { } compass) return null;

		var mapinfo = compass.MapInfo;
		if (mapinfo.IsCleared) return null;

		int current = 0;
		int max = 0;

		if (mapinfo.RequiredDefeatedCount != -1)
		{
			current = mapinfo.GaugeType == 3 ? mapinfo.RequiredDefeatedCount - mapinfo.CurrentDefeatedCount : mapinfo.CurrentDefeatedCount;
			max = mapinfo.RequiredDefeatedCount;
		}
		else if (mapinfo.MapHPMax > 0)
		{
			current = compass.MapHPCurrent > 0 ? compass.MapHPCurrent : mapinfo.MapHPCurrent;
			max = compass.MapHPMax > 0 ? compass.MapHPMax : mapinfo.MapHPMax;
		}

		if (max <= 0) return null;

		string progress = mapinfo.GaugeType switch
		{
			1 => string.Format(BattleRes.ClearProgress, current, max),
			3 => $"{current} / {max} TP",
			_ => $"{current} / {max} HP",
		};

		return mapinfo.CurrentGaugeIndex > 0 ? $"#{mapinfo.CurrentGaugeIndex} {progress}" : progress;
	}
}

/// <summary>The battle detail window: the cell, the map, the enemy fleet and the battles above the boxes.</summary>
public sealed class KcvBattleDetailViewModel : WindowViewModelBase
{
	public string Title => BattleDetailResources.Title;

	public string? NodeLetter { get; }
	public KcvNodeKind NodeKind { get; }
	public string? NodeKindText { get; }
	public string MapText { get; }
	public string? Gauge { get; }
	public string? FleetName { get; }
	public IReadOnlyList<string> Battles { get; }
	public IReadOnlyList<object> Items { get; }

	public IRelayCommand ExpandAllCommand { get; }
	public IRelayCommand CollapseAllCommand { get; }

	public KcvBattleDetailViewModel(LiveBattle.BattleManager bm)
	{
		SortieNode? node = null;
		List<object>? items = null;

		try
		{
			node = KcvLiveBattle.Node(bm);

			if (node is not null)
			{
				KcvBattleResultBlock? practiceResult = bm.IsPractice && bm.Result is not null ? KcvBattleDetailBuilder.Result(bm) : null;
				items = KcvBattleDetailBuilder.Items(node, practiceResult);
			}
		}
		catch (Exception ex)
		{
			Utility.Logger.Add(2, $"{ForkStrings.KcvBattleDetailFallback} ({ex.GetType().Name}: {ex.Message})");
			node = null;
			items = null;
		}

		// the viewer's text in one box when the battle could not be read (heavy air raids, night to day battles)
		items ??=
		[
			new KcvBattleSection(BattleDetailResources.Title,
				[new KcvBattleTextBlock(ForkStrings.KcvBattleDetailFallback, BattleDetailDescriptor.GetBattleDetail(bm).TrimEnd())]),
		];

		if (!bm.IsPractice && bm.Compass is { } compass)
		{
			NodeLetter = compass.CellDisplay;
			NodeKind = KcvSortieRoutes.Kind(compass.EventID, compass.EventKind);
			NodeKindText = KcvRouteText.Kind(compass.EventID, compass.EventKind);
		}

		MapText = KcvLiveBattle.MapText(bm);
		Gauge = KcvLiveBattle.Gauge(bm);
		FleetName = bm.Result?.EnemyFleetName;
		Battles = node switch
		{
			BattleNode b => new[] { b.AirBaseRaid?.Title, b.FirstBattle.Title, b.SecondBattle?.Title }.OfType<string>().ToList(),
			{ AirBaseRaid: { } raid } => [raid.Title],
			_ => new[] { bm.FirstBattle?.BattleName, bm.SecondBattle?.BattleName }.OfType<string>().ToList(),
		};
		Items = items;

		ExpandAllCommand = new RelayCommand(() => SetExpanded(true));
		CollapseAllCommand = new RelayCommand(() => SetExpanded(false));
	}

	private void SetExpanded(bool expanded)
	{
		foreach (KcvBattleSection section in KcvBattleDetailBuilder.Sections(Items)) section.IsExpanded = expanded;
	}
}

/// <summary>A cell of the sortie detail: the step, the cell and its kind, the battles, the rank and the MVP; folded at first.</summary>
public sealed class KcvSortieNodeRow : ObservableObject
{
	public required int Step { get; init; }
	public required string Letter { get; init; }
	public required KcvNodeKind Kind { get; init; }
	public required string KindText { get; init; }
	public string? Battles { get; init; }
	public string? Rank { get; init; }
	public string? Mvp { get; init; }
	public required IReadOnlyList<object> Items { get; init; }

	/// <summary>A cell without a battle or an air raid has nothing to unfold (the viewer's is an empty expander).</summary>
	public bool HasDetail => Items.Count > 0;

	private bool isExpanded;
	public bool IsExpanded
	{
		get => isExpanded;
		set => SetProperty(ref isExpanded, value && HasDetail);
	}
}

/// <summary>The sortie detail window: the map and the start time, the cells in the order the fleet went (the user's answer 1).</summary>
public sealed class KcvSortieDetailViewModel : WindowViewModelBase
{
	/// <summary>The viewer's view model: the cells, and the file menu's commands.</summary>
	public SortieDetailViewModel Detail { get; }

	public string Title => Detail.SortieDetail.Title;
	public string MapText { get; }
	public string? StartTime { get; }
	public IReadOnlyList<KcvSortieNodeRow> Nodes { get; }

	public IRelayCommand ExpandAllCommand { get; }
	public IRelayCommand CollapseAllCommand { get; }

	public KcvSortieDetailViewModel(SortieDetailViewModel detail)
	{
		Detail = detail;

		string? mapName = KCDatabase.Instance.MapInfo[detail.World * 10 + detail.Map]?.NameEN;
		MapText = mapName is null ? $"{detail.World}-{detail.Map}" : $"{mapName} ({detail.World}-{detail.Map})";
		StartTime = detail.StartTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

		Nodes = detail.Nodes
			.Select((node, i) => new KcvSortieNodeRow
			{
				Step = i + 1,
				Letter = node.DisplayCell,
				Kind = KcvSortieRoutes.Kind(node.ApiEventId, node.ApiEventKind),
				KindText = KcvRouteText.Kind(node.ApiEventId, node.ApiEventKind),
				Battles = Battles(node),
				Rank = (node as BattleNode)?.BattleResult?.WinRank,
				Mvp = Mvp(node),
				Items = node is BattleNode || node.AirBaseRaid is not null ? KcvBattleDetailBuilder.Items(node) : [],
			})
			.ToList();

		ExpandAllCommand = new RelayCommand(() => SetExpanded(true));
		CollapseAllCommand = new RelayCommand(() => SetExpanded(false));
	}

	// 「통상 함대 주간전」, 「기지 공습전 → 통상 함대 주간전 → 통상 함대 야전」 as the viewer's node header
	private static string? Battles(SortieNode node)
	{
		string?[] titles = node switch
		{
			BattleNode b => [node.AirBaseRaid?.Title, b.FirstBattle.Title, b.SecondBattle?.Title],
			_ => [node.AirBaseRaid?.Title],
		};

		string[] list = titles.OfType<string>().ToArray();
		return list.Length > 0 ? string.Join(" → ", list) : null;
	}

	private static string? Mvp(SortieNode node)
	{
		if (node is not BattleNode { BattleResult: { } result } battle) return null;

		IFleetData fleet = battle.FirstBattle.FleetsBeforeBattle.Fleet;
		return result.MvpIndex >= 0 && result.MvpIndex < fleet.MembersInstance.Count
			? fleet.MembersInstance[result.MvpIndex]?.Name is { } name ? $"MVP {name}" : null
			: null;
	}

	private void SetExpanded(bool expanded)
	{
		foreach (KcvSortieNodeRow node in Nodes)
		{
			node.IsExpanded = expanded;
			foreach (KcvBattleSection section in KcvBattleDetailBuilder.Sections(node.Items)) section.IsExpanded = expanded;
		}
	}
}

/// <summary>Where the viewer opens its battle detail and sortie detail; true when the window in KanColleViewer's style opened instead.</summary>
public static class KcvBattleDetailWindows
{
	public static bool TryShow(LiveBattle.BattleManager bm)
	{
		if (!KcvViewState.Current.Enabled) return false;

		KcvBattleDetailWindow window = new(new KcvBattleDetailViewModel(bm));
		window.Show(App.Current!.MainWindow!);
		return true;
	}

	public static bool TryShow(SortieDetailViewModel detail)
	{
		if (!KcvViewState.Current.Enabled) return false;

		new KcvSortieDetailWindow(new KcvSortieDetailViewModel(detail)).Show();
		return true;
	}
}
