using KCViewer.Fork;
using KCViewer.Utility;

namespace KCViewer.Window.Wpf.Battle;

/// <summary>
/// Fork: the battle panel's context menu 다음 전투 결과만 표시 and 전투 종료까지 숨기기 (mockup
/// 20261127-battle-result-hide-v3, approved 2026-11-27 "시안 3대로 구현"); <see cref="ForkBattleResultHide"/> decides.
/// 전투 종료까지 숨기기 is the setting of the same name (설정 › 전투, FormBattle.HideDuringBattle), as 드랍 표시 is the
/// log's; while it is on, 다음 전투 결과만 표시 has nothing to do and cannot be chosen.
/// </summary>
public partial class BattleViewModel
{
	private ForkBattleResultHide ForkResultHide { get; } = new();

	public bool ForkHideNextBattle
	{
		get => ForkResultHide.HideNext;
		set
		{
			ForkResultHide.HideNext = value;
			ForkNotifyResultHide();
		}
	}

	public bool ForkHideDuringBattle
	{
		get => Configuration.Config.FormBattle.HideDuringBattle;
		set
		{
			Configuration.Config.FormBattle.HideDuringBattle = value;
			ForkResultHideSettingChanged();
		}
	}

	public bool ForkCanHideNextBattle => !Configuration.Config.FormBattle.HideDuringBattle;

	/// <summary>Whether this response's battle waits for its result (in place of the setting alone).</summary>
	private bool ForkHides(string apiname)
	{
		bool hides = ForkResultHide.Hides(apiname, Configuration.Config.FormBattle.HideDuringBattle);
		ForkNotifyResultHide();
		return hides;
	}

	/// <summary>전투 결과를 임시로 표시.</summary>
	private void ForkShownNow()
	{
		ForkResultHide.ShownNow();
		ForkNotifyResultHide();
	}

	/// <summary>The setting changed here or in the settings window: 다음 전투 결과만 표시 is off while it is on.</summary>
	private void ForkResultHideSettingChanged()
	{
		if (Configuration.Config.FormBattle.HideDuringBattle) ForkResultHide.HideNext = false;
		ForkNotifyResultHide();
	}

	private void ForkNotifyResultHide()
	{
		OnPropertyChanged(nameof(ForkHideNextBattle));
		OnPropertyChanged(nameof(ForkHideDuringBattle));
		OnPropertyChanged(nameof(ForkCanHideNextBattle));
	}
}
