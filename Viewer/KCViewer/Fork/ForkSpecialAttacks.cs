using System.Collections.Generic;
using System.Linq;
using KCViewer.Core.Types.Attacks;
using KCViewer.Data.Battle;
using KCViewer.Data.Battle.Phase;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the special attacks (Nelson Touch, 長門 and the others) the player's fleet made in a battle, for the battle
/// panel's rows and the compass's cells (mockup 20261123-special-attack-v2, approved 2026-11-23). The ships taking part
/// are the viewer's own (SpecialAttackParticipationIndexes: the submarine tender flagship with its submarines too), in
/// the fleet that attacked; a combined fleet's night battle is its
/// escort's, as the viewer's night battle has it (PhaseNightBattle.EmulateBattle). The night 瑞雲 cut-in, which the
/// viewer counts with them for its battle lines, is one ship's cut-in and left out; so are the friendly fleet's attacks
/// (PhaseFriendlyShelling, not a shelling or night battle phase) and an exercise opponent's.
/// </summary>
public static class ForkSpecialAttacks
{
	public sealed class Result
	{
		public static Result None { get; } = new([], []);

		private Result(HashSet<int> ships, List<string> names)
		{
			Ships = ships;
			Names = names;
		}

		/// <summary>The ships taking part, as battle indexes (BattleIndex: friend main 0-6, friend escort 6-11).</summary>
		public IReadOnlySet<int> Ships { get; }

		/// <summary>The special attacks by the viewer's names (AttackResources), each once, in the order made.</summary>
		public IReadOnlyList<string> Names { get; }

		public bool Any => Names.Count > 0;

		internal static Result Of(HashSet<int> ships, List<string> names) => names.Count > 0 ? new(ships, names) : None;
	}

	/// <summary>The special attacks of the battles given (a day battle and its night battle), the nulls left out.</summary>
	public static Result Find(params BattleData?[] battles)
	{
		HashSet<int> ships = [];
		List<string> names = [];

		foreach (BattleData battle in battles.OfType<BattleData>())
		{
			foreach (PhaseBase phase in battle.GetPhases())
			{
				if (!phase.IsAvailable) continue;

				switch (phase)
				{
					case PhaseShelling { Attacks: { } attacks }:
						foreach (PhaseShelling.PhaseShellingAttack attack in attacks)
						{
							if (!attack.Attacker.IsFriend || !attack.AttackTypeTyped.IsSpecialAttack()) continue;

							Add(attack.Attacker.Side,
								((DayAttackKind?)attack.AttackTypeTyped).SpecialAttackParticipationIndexes(),
								DayAttack.AttackDisplay(attack.AttackTypeTyped));
						}
						break;

					case PhaseNightBattle { Attacks: { } attacks }:
						foreach (PhaseNightBattle.PhaseNightBattleAttack attack in attacks)
						{
							if (!attack.Attacker.IsFriend
								|| attack.AttackTypeTyped is NightAttackKind.CutinZuiun
								|| !attack.AttackTypeTyped.IsSpecialAttack()) continue;

							Add(battle.IsFriendCombined ? BattleSides.FriendEscort : attack.Attacker.Side,
								((NightAttackKind?)attack.AttackTypeTyped).SpecialAttackParticipationIndexes(),
								NightAttack.AttackDisplay(attack.AttackTypeTyped));
						}
						break;
				}
			}
		}

		return Result.Of(ships, names);

		void Add(BattleSides side, List<int> indexes, string name)
		{
			foreach (int index in indexes)
			{
				ships.Add(BattleIndex.Get(side, index));
			}

			if (!names.Contains(name))
			{
				names.Add(name);
			}
		}
	}
}
