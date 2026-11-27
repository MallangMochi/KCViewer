using KCViewer.Fork;
using Xunit;

namespace KCViewerCoreTests;

/// <summary>
/// Fork: when the battle panel hides a battle until its result (KCViewer/Fork/ForkBattleResultHide.cs, mockup
/// 20261127-battle-result-hide-v3 ②: J then the boss K with a night battle).
/// </summary>
public class ForkBattleResultHideTests
{
	private const string Next = "api_req_map/next";
	private const string Day = "api_req_sortie/battle";
	private const string Night = "api_req_battle_midnight/battle";
	private const string Result = "api_req_sortie/battleresult";
	private const string Port = "api_port/port";

	[Fact]
	public void ByDefaultEveryBattleShowsAtOnce()
	{
		ForkBattleResultHide hide = new();

		Assert.False(hide.Hides(Day, false));
		Assert.False(hide.Hides(Result, false));
		Assert.False(hide.Hides(Day, false));
		Assert.False(hide.Hides(Night, false));
	}

	[Fact]
	public void HideNextHidesOneBattleUntilItsResultAndTurnsItselfOff()
	{
		ForkBattleResultHide hide = new() { HideNext = true };

		hide.Hides(Next, false);
		Assert.True(hide.Hides(Day, false)); // J
		Assert.True(hide.HideNext);
		Assert.False(hide.Hides(Result, false));
		Assert.False(hide.HideNext);

		hide.Hides(Next, false);
		Assert.False(hide.Hides(Day, false)); // K as usual
		Assert.False(hide.Hides(Night, false));
	}

	[Fact]
	public void HideNextKeepsTheNightBattleOfTheBattleItHides()
	{
		ForkBattleResultHide hide = new() { HideNext = true };

		Assert.True(hide.Hides(Day, false));
		Assert.True(hide.Hides(Night, false));
		Assert.False(hide.Hides(Result, false));
	}

	[Fact]
	public void HideNextChosenDuringAShownBattleWaitsForTheNextOne()
	{
		ForkBattleResultHide hide = new();

		Assert.False(hide.Hides(Day, false));
		hide.HideNext = true;
		Assert.False(hide.Hides(Night, false)); // the battle already showing
		Assert.False(hide.Hides(Result, false));
		Assert.True(hide.HideNext);

		Assert.True(hide.Hides(Day, false));
	}

	[Theory]
	[InlineData("api_req_sortie/ld_airbattle", "api_req_sortie/battleresult")] // a fleet air raid has a result
	[InlineData("api_req_practice/battle", "api_req_practice/battle_result")]
	[InlineData("api_req_combined_battle/each_battle", "api_req_combined_battle/battleresult")]
	[InlineData("api_req_battle_midnight/sp_midnight", "api_req_sortie/battleresult")]
	public void HideNextCoversEveryKindOfBattle(string start, string result)
	{
		ForkBattleResultHide hide = new() { HideNext = true };

		Assert.True(hide.Hides(start, false));
		Assert.False(hide.Hides(result, false));
		Assert.False(hide.HideNext);
	}

	[Fact]
	public void ABaseAirRaidFollowsTheSettingAndLeavesHideNextForTheBattle()
	{
		ForkBattleResultHide hide = new() { HideNext = true };

		Assert.False(hide.Hides(Next, false)); // 기지항공대 공습 at the cell
		Assert.False(hide.Hides("api_req_map/air_raid", false));
		Assert.True(hide.HideNext);
		Assert.True(hide.Hides(Day, false));

		Assert.True(new ForkBattleResultHide().Hides(Next, true));
	}

	[Fact]
	public void HideNextStaysThroughThePortWithoutABattle()
	{
		ForkBattleResultHide hide = new() { HideNext = true };

		hide.Hides(Port, false);
		Assert.True(hide.HideNext);
		Assert.True(hide.Hides(Day, false));
	}

	[Fact]
	public void TheSettingHidesEveryBattle()
	{
		ForkBattleResultHide hide = new();

		Assert.True(hide.Hides(Day, true));
		Assert.True(hide.Hides(Night, true));
		Assert.False(hide.Hides(Result, true));
		Assert.True(hide.Hides(Day, true));
	}

	[Fact]
	public void ShowingTheHiddenBattleUsesHideNextUp()
	{
		ForkBattleResultHide hide = new() { HideNext = true };

		Assert.True(hide.Hides(Day, false));
		hide.ShownNow();
		Assert.False(hide.HideNext);
		Assert.False(hide.Hides(Night, false));
	}

	[Fact]
	public void ShowingABattleShownAlreadyLeavesHideNextOn()
	{
		ForkBattleResultHide hide = new();

		hide.Hides(Day, false);
		hide.HideNext = true;
		hide.ShownNow();
		Assert.True(hide.HideNext);
	}
}
