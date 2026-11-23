using System;
using System.Collections.Generic;
using System.Linq;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Data.Battle;
using KCViewer.Fork;
using KCViewer.Fork.KcvViews;
using KCViewer.Observer;
using KCViewer.Window.Wpf.Compass.ViewModels;

namespace KCViewer.Window.Wpf.Compass;

/// <summary>
/// Fork: what the KanColleViewer compass panel (Fork/KcvViews/KcvCompassView.xaml, user decisions 2026-10-01) shows
/// beyond the viewer's own texts: the map number and name, the next cell as its letter (the cell number in the
/// tooltip), the cells passed in this sortie, the enemy fleets as lists. The cell, its kind and the route are
/// coloured by the kind of cell as the sortie record's route (mockup 20261005-compass-route-v4, user decisions
/// 2026-10-05). The cells of a special attack get a gold ring on the route and on the cell (mockup
/// 20261123-special-attack-v2). Called at the end of <see cref="Updated"/> and when a battle starts.
/// </summary>
public partial class CompassViewModel
{
	public enum ForkCompassMode
	{
		None,
		Sortie,
		Exercise,
	}

	public ForkCompassMode ForkMode { get; private set; }
	public bool ForkIsSortie => ForkMode == ForkCompassMode.Sortie;
	public bool ForkIsExercise => ForkMode == ForkCompassMode.Exercise;

	/// <summary>1-1, with the difficulty on event maps (1-1 [甲]).</summary>
	public string? ForkMapNumber { get; private set; }
	public string? ForkMapName { get; private set; }

	/// <summary>The cell's letter (B, G2, ZZ3), or its number when no letter is known.</summary>
	public string? ForkCellText { get; private set; }

	/// <summary>1-1-2 (B); 7-5-12 when no letter is known. The end node and the reconnaissance result follow.</summary>
	public string? ForkCellToolTip { get; private set; }

	/// <summary>The kind of the cell, for the colour of its letter and of the kind of cell (Kcv.Node).</summary>
	public KcvNodeKind ForkCellKind { get; private set; }

	/// <summary>
	/// A special attack was made in the battle of the cell the fleet is at: its gold ring (mockup
	/// 20261123-special-attack-v2); the attack is the last line of <see cref="ForkCellToolTip"/>.
	/// </summary>
	public bool ForkCellSpecialAttack { get; private set; }

	/// <summary>The cell's tooltip without the special attacks (<see cref="ForkSetCell"/>).</summary>
	private string? ForkCellBaseToolTip { get; set; }

	/// <summary>The cells passed in this sortie, the last one the cell the fleet is at.</summary>
	public IReadOnlyList<KcvRouteNode> ForkRoute { get; private set; } = [];
	public bool ForkShowRoute => ForkIsSortie && ForkRoute.Count > 0;

	public string? ForkOpponentName { get; private set; }
	public string? ForkOpponentRank { get; private set; }

	/// <summary>The text beside the kind of cell; left out when the enemy fleets below show their names.</summary>
	public bool ForkShowDetail { get; private set; }

	public bool ForkShowCandidates { get; private set; }
	public bool ForkShowEnemy { get; private set; }
	public IReadOnlyList<EnemyFleetElementViewModel> ForkCandidates { get; private set; } = [];
	public IReadOnlyList<MasterShipViewModel> ForkEnemyShips { get; private set; } = [];

	private sealed class ForkStep
	{
		public required string Letter { get; init; }
		public required int EventId { get; init; }
		public required int EventKind { get; init; }
		public string? Items { get; init; }
		public string? Rank { get; set; }

		/// <summary>The special attacks of the cell's battle by name, one a line; null when none was made.</summary>
		public string? SpecialAttack { get; set; }
	}

	private List<ForkStep> ForkSteps { get; } = [];

	/// <summary>
	/// Called at the end of the constructor: the battle results give the rank of the route's last cell, and the night
	/// battles after a day battle (which the viewer's compass does not follow) their special attacks.
	/// </summary>
	private void ForkInitialize()
	{
		APIObserver o = APIObserver.Instance;
		o.ApiReqSortie_BattleResult.ResponseReceived += ForkBattleResult;
		o.ApiReqCombinedBattle_BattleResult.ResponseReceived += ForkBattleResult;

		o.ApiReqBattleMidnight_Battle.ResponseReceived += ForkNightBattle;
		o.ApiReqCombinedBattle_MidnightBattle.ResponseReceived += ForkNightBattle;
		o.ApiReqCombinedBattle_EcMidnightBattle.ResponseReceived += ForkNightBattle;
	}

	private void ForkUpdated(string apiname, object? data)
	{
		switch (apiname)
		{
			case "api_port/port":
				ForkMode = ForkCompassMode.None;
				break;

			case "api_req_member/get_practice_enemyinfo" when data is not null:
				dynamic d = data;
				ForkMode = ForkCompassMode.Exercise;
				ForkOpponentName = (string)d.api_nickname;
				ForkOpponentRank = Constants.GetAdmiralRank((int)d.api_rank);
				break;

			case "api_req_map/start":
				ForkSteps.Clear();
				ForkSetCell();
				ForkAddStep();
				break;

			case "api_req_map/next":
				ForkSetCell();
				ForkAddStep();
				break;

			default:
				ForkSetCell();
				break;
		}

		ForkUpdateCellSpecialAttack();
		ForkSetBody();
		OnPropertyChanged(nameof(ForkShowRoute));
	}

	private void ForkBattleStarted(bool isPractice)
	{
		ForkMode = isPractice ? ForkCompassMode.Exercise : ForkCompassMode.Sortie;
		ForkSetSpecialAttack();
		ForkSetBody();
		OnPropertyChanged(nameof(ForkShowRoute));
	}

	private void ForkNightBattle(string apiname, dynamic data) => ForkSetSpecialAttack();

	/// <summary>
	/// The special attacks of the battle of the cell the fleet is at, its day and night battle (mockup
	/// 20261123-special-attack-v2): kept on the route's last cell for the rest of the sortie, and shown on the cell.
	/// </summary>
	private void ForkSetSpecialAttack()
	{
		if (!ForkIsSortie || ForkSteps.Count is 0) return;

		BattleManager battle = Db.Battle;
		ForkSpecialAttacks.Result special = ForkSpecialAttacks.Find(battle.BattleDay, battle.BattleNight);

		ForkSteps[^1].SpecialAttack = special.Any ? string.Join("\n", special.Names) : null;
		ForkRefreshRoute();
		ForkUpdateCellSpecialAttack();
	}

	/// <summary>The cell the fleet is at, the route's last cell: its gold ring and the attacks in its tooltip.</summary>
	private void ForkUpdateCellSpecialAttack()
	{
		string? special = ForkIsSortie && ForkSteps.Count > 0 ? ForkSteps[^1].SpecialAttack : null;

		ForkCellSpecialAttack = special is not null;
		ForkCellToolTip = special is null
			? ForkCellBaseToolTip
			: ForkCellBaseToolTip + Environment.NewLine + special.Replace("\n", Environment.NewLine);
	}

	private void ForkSetCell()
	{
		CompassData? compass = Db.Battle.Compass;
		if (compass is null) return;

		ForkMode = ForkCompassMode.Sortie;

		IMapInfoData? map = compass.MapInfo;
		int difficulty = map?.EventDifficulty ?? 0;
		ForkMapNumber = $"{compass.MapAreaID}-{compass.MapInfoID}" + (difficulty > 0 ? $" [{Constants.GetDifficulty(difficulty)}]" : "");
		ForkMapName = map?.NameEN;

		string label = compass.CellDisplay;
		bool hasLetter = label != compass.CellId.ToString();
		ForkCellText = label;

		List<string> tip = [$"{compass.MapAreaID}-{compass.MapInfoID}-{compass.CellId}" + (hasLetter ? $" ({label})" : "")];
		if (compass.IsEndPoint) tip.Add(ForkStrings.KcvEndNode);
		if (!string.IsNullOrEmpty(TextDestinationToolTip)) tip.Add(TextDestinationToolTip);
		ForkCellBaseToolTip = string.Join(Environment.NewLine, tip);

		ForkCellKind = KcvSortieRoutes.Kind(compass.EventID, compass.EventKind);
	}

	// The cell the fleet moved to, with the items got there (as the viewer's compass writes them) or, at a
	// maelstrom, the viewer's text of the loss with its share 「탄약 x 20 (35%)」
	private void ForkAddStep()
	{
		CompassData? compass = Db.Battle.Compass;
		if (compass is null) return;

		ForkSteps.Add(new ForkStep
		{
			Letter = compass.CellDisplay,
			EventId = compass.EventID,
			EventKind = compass.EventKind,
			Items = compass.EventID is 3 ? TextEventDetailText : KcvRouteText.Items(compass.GetItems),
		});
		ForkRefreshRoute();
	}

	private void ForkBattleResult(string apiname, dynamic data)
	{
		if (ForkSteps.Count is 0 || !data.api_win_rank()) return;

		ForkSteps[^1].Rank = (string)data.api_win_rank;
		ForkRefreshRoute();
	}

	// The tooltip's last lines are the special attacks of the cell's battle (mockup 20261123-special-attack-v2)
	private void ForkRefreshRoute()
	{
		ForkRoute = ForkSteps
			.Select((s, i) => new KcvRouteNode(s.Letter, KcvSortieRoutes.Kind(s.EventId, s.EventKind),
				KcvRouteText.ToolTip(s.Letter, s.EventId, s.EventKind, s.Items, s.Rank) + (s.SpecialAttack is { } special ? "\n" + special : ""),
				i == 0, i == ForkSteps.Count - 1, s.SpecialAttack is not null))
			.ToList();
		OnPropertyChanged(nameof(ForkShowRoute));
	}

	private void ForkSetBody()
	{
		ForkShowCandidates = ForkMode == ForkCompassMode.Sortie && CurrentViewModel is EnemyListViewModel;
		ForkShowEnemy = ForkMode != ForkCompassMode.None && CurrentViewModel is BattleViewModel;

		ForkCandidates = ForkShowCandidates ? EnemyFleetCandidate.ToList() : [];
		ForkEnemyShips = ForkShowEnemy ? EnemyFleet?.Where(s => s.ShipId > 0).ToList() ?? [] : [];

		ForkShowDetail = !string.IsNullOrEmpty(TextEventDetailText) && !(ForkShowCandidates && ForkCandidates.Count > 0);
	}
}
