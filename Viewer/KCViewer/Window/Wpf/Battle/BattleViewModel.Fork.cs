using System;
using System.Collections.Generic;
using System.Globalization;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Data.Battle;
using KCViewer.Fork;
using KCViewer.Fork.KcvViews;
using KCViewer.Utility;
using KCViewer.Window.Wpf.Battle.ViewModels;

namespace KCViewer.Window.Wpf.Battle;

/// <summary>What the battle panel's drop box shows (mockup 20261006-battle-drop-v2).</summary>
public enum ForkDropState
{
	/// <summary>No box: before the result, an exercise, 드랍함 표시 off, or nothing dropped while spoilers are hidden.</summary>
	Hidden,
	Ship,
	/// <summary>No ship of its line is owned: the name in the orange badge of the quest panel's 수행 중.</summary>
	NewShip,
	/// <summary>Nothing dropped: 「없음」, faint.</summary>
	None,
	/// <summary>스포일러 허가 off: 「스포일러」, the name in the tooltip, no badge.</summary>
	Spoiler,
}

/// <summary>
/// Fork: what the KanColleViewer battle panel (Fork/KcvViews/KcvBattleView.xaml) shows beyond the viewer's own:
/// the battle's name and time, and each bar's ship name and type. Called at the start of SetHPBar.
/// </summary>
public partial class BattleViewModel
{
	// The dropped ship in the top right (mockup 20261006-battle-drop-v2, approved 2026-10-06 "함급 표시는 필요 없어보임
	// 제거하고 구현": the name only, no ship type). 드랍함 표시 is the viewer's log setting 드랍 보고 표시
	// (Log.ShowDropSpoiler); with 스포일러 허가 (Log.ShowSpoiler) off the name is hidden as 「스포일러」.

	/// <summary>The current battle's result has come (the box shows from then until the next battle starts).</summary>
	private bool ForkHasResult { get; set; }
	private bool ForkResultIsPractice { get; set; }
	private IShipDataMaster? ForkDroppedShip { get; set; }
	private bool ForkDroppedShipIsNew { get; set; }

	/// <summary>The lines (base ships) dropped since the port: not in the ship list yet, but no longer a first join.</summary>
	private HashSet<int> ForkDroppedLines { get; } = [];

	public ForkDropState ForkDropState { get; private set; }
	public string? ForkDropText { get; private set; }
	public string? ForkDropToolTip { get; private set; }

	/// <summary>Hidden with the battle information too (戦闘終了まで隠す).</summary>
	public bool ForkDropVisible => ViewVisible && ForkDropState is not ForkDropState.Hidden;

	/// <summary>The context menu's 드랍함 표시, the same value as the log setting 드랍 보고 표시.</summary>
	public bool ForkShowDrop
	{
		get => Configuration.Config.Log.ShowDropSpoiler;
		set
		{
			Configuration.Config.Log.ShowDropSpoiler = value;
			ForkUpdateDrop();
		}
	}

	/// <summary>At a battle result (sortie, combined fleet or exercise).</summary>
	private void ForkSetDrop(BattleManager bm)
	{
		ForkHasResult = true;
		ForkResultIsPractice = bm.IsPractice;
		ForkDroppedShip = bm.Result?.DroppedShipID is int id and > 0 ? KCDatabase.Instance.MasterShips[id] : null;
		ForkDroppedShipIsNew = ForkDroppedShip is { } ship && KcvShipCollection.IsMissing(ship, ForkDroppedLines);

		if (ForkDroppedShip is { } dropped)
		{
			ForkDroppedLines.Add(KcvShipCollection.BaseShipId(dropped));
		}

		ForkUpdateDrop();
	}

	/// <summary>At the port: the ships dropped are in the ship list from now on.</summary>
	private void ForkClearDrops()
	{
		ForkDroppedLines.Clear();
		ForkHasResult = false;
		ForkUpdateDrop();
	}

	/// <summary>From the result and the two log settings; also when the settings change.</summary>
	private void ForkUpdateDrop()
	{
		Configuration.ConfigurationData.ConfigLog log = Configuration.Config.Log;

		ForkDropState = (ForkHasResult, ForkResultIsPractice, log.ShowDropSpoiler, log.ShowSpoiler, ForkDroppedShip) switch
		{
			(false, _, _, _, _) or (_, true, _, _, _) or (_, _, false, _, _) => ForkDropState.Hidden,
			(_, _, _, false, null) => ForkDropState.Hidden,
			(_, _, _, false, _) => ForkDropState.Spoiler,
			(_, _, _, true, null) => ForkDropState.None,
			_ => ForkDroppedShipIsNew ? ForkDropState.NewShip : ForkDropState.Ship,
		};
		ForkDropText = ForkDropState switch
		{
			ForkDropState.None => ForkStrings.KcvDropNone,
			ForkDropState.Spoiler => ForkStrings.KcvDropSpoiler,
			ForkDropState.Ship or ForkDropState.NewShip => ForkDroppedShip?.NameEN,
			_ => null,
		};
		ForkDropToolTip = ForkDropState is ForkDropState.Spoiler ? ForkDroppedShip?.NameEN : null;
		OnPropertyChanged(nameof(ForkShowDrop));
	}

	/// <summary>For example 通常艦隊昼戦, 演習昼戦 (BattleData.BattleName, translated).</summary>
	public string? ForkBattleName { get; private set; }

	public string? ForkBattleTime { get; private set; }

	// The bars of this battle by fleet, one column each on the KanColleViewer panel: friendly main (with a 7th ship),
	// friendly escort, enemy escort, enemy main. Built here rather than read from PlayerMainHPBars and the others,
	// which move the 7th ship by a setting and keep the escort bars of an earlier combined battle.
	public IReadOnlyList<HealthBarViewModel> ForkFriendMainBars { get; private set; } = [];
	public IReadOnlyList<HealthBarViewModel> ForkFriendEscortBars { get; private set; } = [];
	public IReadOnlyList<HealthBarViewModel> ForkEnemyEscortBars { get; private set; } = [];
	public IReadOnlyList<HealthBarViewModel> ForkEnemyMainBars { get; private set; } = [];

	public bool ForkFriendCombined { get; private set; }
	public bool ForkEnemyCombined { get; private set; }

	private void ForkSetBattle(BattleData bd)
	{
		// a new battle (or its next phase) before its result: no drop box
		ForkHasResult = false;
		ForkUpdateDrop();

		ForkBattleName = bd.BattleName;
		ForkBattleTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

		var initial = bd.Initial;
		List<HealthBarViewModel> friendMain = [];
		List<HealthBarViewModel> friendEscort = [];
		List<HealthBarViewModel> enemyMain = [];
		List<HealthBarViewModel> enemyEscort = [];

		// The same slots as SetHPBar fills
		for (int i = 0; i < initial.FriendInitialHPs.Length; i++)
		{
			if (initial.FriendInitialHPs[i] == -1) continue;

			HealthBarViewModel bar = HPBars[BattleIndex.Get(BattleSides.FriendMain, i)];
			if (bd.IsBaseAirRaid)
			{
				SetShip(friendMain, bar, string.Format(FormBattle.AirBase, i + 1), null);
			}
			else
			{
				var ship = initial.FriendFleet?.MembersInstance?[i];
				SetShip(friendMain, bar, ship?.Name, ship?.MasterShip.ShipTypeName);
			}
		}

		for (int i = 0; i < initial.EnemyInitialHPs.Length; i++)
		{
			if (initial.EnemyInitialHPs[i] == -1) continue;

			var ship = initial.EnemyMembersInstance[i];
			SetShip(enemyMain, HPBars[BattleIndex.Get(BattleSides.EnemyMain, i)], ship?.NameWithClass, ship?.ShipTypeName);
		}

		if (bd.IsFriendCombined && initial.FriendInitialHPsEscort is { } friendEscortHPs)
		{
			for (int i = 0; i < friendEscortHPs.Length; i++)
			{
				if (friendEscortHPs[i] == -1) continue;

				var ship = initial.FriendFleetEscort.MembersInstance![i];
				SetShip(friendEscort, HPBars[BattleIndex.Get(BattleSides.FriendEscort, i)], ship?.Name, ship?.MasterShip.ShipTypeName);
			}
		}

		if (bd.IsEnemyCombined && initial.EnemyInitialHPsEscort is { } enemyEscortHPs)
		{
			for (int i = 0; i < enemyEscortHPs.Length; i++)
			{
				if (enemyEscortHPs[i] == -1) continue;

				var ship = initial.EnemyMembersEscortInstance![i];
				SetShip(enemyEscort, HPBars[BattleIndex.Get(BattleSides.EnemyEscort, i)], ship?.NameWithClass, ship?.ShipTypeName);
			}
		}

		ForkFriendMainBars = friendMain;
		ForkFriendEscortBars = friendEscort;
		ForkEnemyEscortBars = enemyEscort;
		ForkEnemyMainBars = enemyMain;
		ForkFriendCombined = friendEscort.Count > 0;
		ForkEnemyCombined = enemyEscort.Count > 0;

		static void SetShip(List<HealthBarViewModel> column, HealthBarViewModel bar, string? name, string? type)
		{
			bar.ShipName = name;
			bar.ShipTypeName = type;
			column.Add(bar);
		}
	}
}
