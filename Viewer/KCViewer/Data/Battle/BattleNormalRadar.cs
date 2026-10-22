using System.Collections.Generic;
using KCViewer.Data.Battle.Phase;

namespace KCViewer.Data.Battle;

/// <summary>
/// 通常艦隊 vs 通常艦隊 レーダー射撃
/// </summary>
public class BattleNormalRadar : BattleDay
{
	public override void LoadFromResponse(string apiname, dynamic data)
	{
		base.LoadFromResponse(apiname, (object)data);

		JetBaseAirAttack = new PhaseJetBaseAirAttack(this, "噴式基地航空隊攻撃");
		BaseAirAttack = new PhaseBaseAirAttack(this, "基地航空隊攻撃");
		Shelling1 = new PhaseRadar(this, "レーダー射撃");

		foreach (var phase in GetPhases())
			phase.EmulateBattle(_resultHPs, _attackDamages);
	}

	public override string APIName => "api_req_sortie/ld_shooting";

	// Fork: the translated name (as the sortie record's BattleNormalRadar) instead of the Japanese text
	public override string BattleName => ConstantsRes.Title_NormalRadar;

	public override IEnumerable<PhaseBase> GetPhases()
	{
		yield return Initial;
		yield return Searching;     // ?
		yield return JetBaseAirAttack;
		yield return BaseAirAttack;
		yield return Shelling1;
	}
}
