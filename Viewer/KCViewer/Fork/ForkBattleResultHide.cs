using System.Collections.Generic;

namespace KCViewer.Fork;

/// <summary>
/// Fork: when the battle panel shows a battle (mockup 20261127-battle-result-hide-v3, approved 2026-11-27 "시안 3대로
/// 구현"). The viewer shows a battle as soon as it starts; with 전투 종료까지 숨기기 (FormBattle.HideDuringBattle, on the
/// panel's context menu too) every battle waits for its result. 다음 전투 결과만 표시 (<see cref="HideNext"/>) does so
/// for one battle: the first battle that starts after it was chosen, day and night, until its result, where it turns
/// itself off. A battle already showing when it is chosen stays shown. 기지항공대 공습 (no result) follows the setting only
/// and leaves it for the next battle. 전투 결과를 임시로 표시 shows the battle it hides and so uses it up.
/// </summary>
public sealed class ForkBattleResultHide
{
	/// <summary>The night battles that go on from a day battle, part of the battle that started with it.</summary>
	private static readonly HashSet<string> NightContinuations =
	[
		"api_req_battle_midnight/battle",
		"api_req_practice/midnight_battle",
		"api_req_combined_battle/midnight_battle",
		"api_req_combined_battle/ec_midnight_battle",
	];

	/// <summary>The battles that start a battle (fleet air raids included: they have a result).</summary>
	private static readonly HashSet<string> Starts =
	[
		"api_req_sortie/battle",
		"api_req_practice/battle",
		"api_req_sortie/airbattle",
		"api_req_sortie/ld_airbattle",
		"api_req_sortie/ld_shooting",
		"api_req_sortie/night_to_day",
		"api_req_battle_midnight/sp_midnight",
		"api_req_combined_battle/battle",
		"api_req_combined_battle/battle_water",
		"api_req_combined_battle/airbattle",
		"api_req_combined_battle/ld_airbattle",
		"api_req_combined_battle/ec_battle",
		"api_req_combined_battle/each_battle",
		"api_req_combined_battle/each_battle_water",
		"api_req_combined_battle/ld_shooting",
		"api_req_combined_battle/sp_midnight",
		"api_req_combined_battle/ec_night_to_day",
	];

	private static readonly HashSet<string> Results =
	[
		"api_req_sortie/battleresult",
		"api_req_combined_battle/battleresult",
		"api_req_practice/battle_result",
	];

	/// <summary>다음 전투 결과만 표시: on until the result of the battle it hides. Not kept when the viewer closes.</summary>
	public bool HideNext { get; set; }

	/// <summary>The battle on the panel is hidden by <see cref="HideNext"/>.</summary>
	public bool HidingThisBattle { get; private set; }

	private bool InBattle { get; set; }

	/// <summary>
	/// Whether the panel hides what this response shows (it shows at the result); <paramref name="hideDuringBattle"/> is
	/// 전투 종료까지 숨기기.
	/// </summary>
	public bool Hides(string apiname, bool hideDuringBattle)
	{
		if (Results.Contains(apiname))
		{
			if (HidingThisBattle) HideNext = false;
			HidingThisBattle = false;
			InBattle = false;
			return false;
		}

		bool continuation = NightContinuations.Contains(apiname);
		if (!continuation && !Starts.Contains(apiname))
		{
			// the port, a cell, 기지항공대 공습: as the setting says
			if (apiname == "api_port/port")
			{
				HidingThisBattle = false;
				InBattle = false;
			}

			return hideDuringBattle;
		}

		if (!InBattle || !continuation)
		{
			InBattle = true;
			HidingThisBattle = HideNext && !hideDuringBattle;
		}

		return hideDuringBattle || HidingThisBattle;
	}

	/// <summary>전투 결과를 임시로 표시: the battle hidden by <see cref="HideNext"/> is shown, which uses it up.</summary>
	public void ShownNow()
	{
		if (!HidingThisBattle) return;

		HidingThisBattle = false;
		HideNext = false;
	}
}
