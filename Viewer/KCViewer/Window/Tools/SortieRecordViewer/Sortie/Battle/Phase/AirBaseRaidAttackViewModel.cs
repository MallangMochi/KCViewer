using System;
using System.Linq;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Attacks;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;

public sealed class AirBaseRaidAttackViewModel : AttackViewModelBase
{
	private int WaveIndex { get; }

	public BattleIndex DefenderIndex { get; }
	public IBaseAirCorpsData Defender { get; }
	public int DefenderHpBeforeAttack { get; }

	// Fork: public for the battle detail in KanColleViewer's style (Fork/KcvViews/KcvBattleDetail.cs)
	public double Damage { get; }
	public HitType HitType { get; }
	public AirAttack AttackType { get; }
	public bool GuardsFlagship { get; }
	public string AttackKind => GetAttackKind(AttackType);
	public string DamageDisplay { get; }

	public string AttackerName => (WaveIndex, DefenderIndex.FleetFlag) switch
	{
		(> 0, _) => string.Format(BattleRes.AirSquadronWave, WaveIndex),
		(_, FleetFlag.Player) => BattleRes.EnemyAirSquadron,
		(_, FleetFlag.Enemy) => BattleRes.FriendlyAirSquadron,
		_ => "???",
	};

	public AirBaseRaidAttackViewModel(BattleFleets fleets, int waveIndex, AirBattleAttack attack)
	{
		WaveIndex = waveIndex;
		Damage = attack.Defenders.First().Damage;
		HitType = attack.Defenders.First().CriticalFlag;

		DefenderIndex = attack.Defenders.First().Defender;
		Defender = fleets.GetAirBase(DefenderIndex)!;
		AttackType = attack.AttackType;
		GuardsFlagship = attack.Defenders.First().GuardsFlagship;

		DamageDisplay =
			$"[{GetAttackKind(AttackType)}] " +
			$"{AttackDisplay(attack.Defenders.First().GuardsFlagship, Damage, HitType)}";

		DefenderHpBeforeAttack = Defender.HPCurrent;

		int hpAfterAttacks = Math.Max(0, DefenderHpBeforeAttack - (int)Damage);

		if (DefenderHpBeforeAttack > 0 && DefenderHpBeforeAttack != hpAfterAttacks)
		{
			DamageDisplay += $" ({DefenderHpBeforeAttack} → {hpAfterAttacks})";
		}
	}

	private static string GetAttackKind(AirAttack airAttack) => airAttack switch
	{
		AirAttack.Torpedo => ConstantsRes.TorpedoAttack,
		AirAttack.Bombing => ConstantsRes.BombingAttack,
		AirAttack.TorpedoBombing => ConstantsRes.TorpBombingAttack,
		_ => ConstantsRes.Unknown,
	};
}
