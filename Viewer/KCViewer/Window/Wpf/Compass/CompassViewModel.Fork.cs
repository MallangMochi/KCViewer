using System;
using System.Collections.Generic;
using System.Linq;
using KCViewer.Core.Types;
using KCViewer.Data;
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
/// 2026-10-05). Called at the end of <see cref="Updated"/> and when a battle starts.
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
	}

	private List<ForkStep> ForkSteps { get; } = [];

	/// <summary>Called at the end of the constructor: the battle results give the rank of the route's last cell.</summary>
	private void ForkInitialize()
	{
		APIObserver o = APIObserver.Instance;
		o.ApiReqSortie_BattleResult.ResponseReceived += ForkBattleResult;
		o.ApiReqCombinedBattle_BattleResult.ResponseReceived += ForkBattleResult;
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

		ForkSetBody();
		OnPropertyChanged(nameof(ForkShowRoute));
	}

	private void ForkBattleStarted(bool isPractice)
	{
		ForkMode = isPractice ? ForkCompassMode.Exercise : ForkCompassMode.Sortie;
		ForkSetBody();
		OnPropertyChanged(nameof(ForkShowRoute));
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
		ForkCellToolTip = string.Join(Environment.NewLine, tip);

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

	private void ForkRefreshRoute()
	{
		ForkRoute = ForkSteps
			.Select((s, i) => new KcvRouteNode(s.Letter, KcvSortieRoutes.Kind(s.EventId, s.EventKind),
				KcvRouteText.ToolTip(s.Letter, s.EventId, s.EventKind, s.Items, s.Rank), i == 0, i == ForkSteps.Count - 1))
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
