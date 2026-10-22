using System.Collections.Generic;
using System.Linq;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;
using KCViewer.Database;
using KCViewer.Database.Sortie;
using KCViewer.Services;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Node;
using KCViewer.Window.Tools.SortieRecordViewer.SortieDetail;

namespace KCViewer.Window.Tools.SortieRecordViewer.SortieCostViewer;

public class JetCostCalculator(KCViewerContext db, ToolService toolService, SortieRecordViewModel sortie)
{
	private KCViewerContext Db { get; } = db;
	private ToolService ToolService { get; } = toolService;

	private SortieRecord Model { get; } = sortie.Model;

	private SortieDetailViewModel? SortieDetails { get; set; }

	public SortieCostModel FleetJetCost() => FleetJetCost(Db, Model);
	
	public SortieCostModel AirBaseJetCost() => AirBaseJetCost(Db, Model);

	private SortieCostModel FleetJetCost(KCViewerContext db, SortieRecord model)
	{
		if (model.CalculatedSortieCost.SortieFleetJetCost is not null)
		{
			return model.CalculatedSortieCost.SortieFleetJetCost;
		}

		SortieDetails ??= ToolService.GenerateSortieDetailViewModel(db, model);

		if (SortieDetails is null) return SortieCostModel.Zero;
		if (CalculateFleetJetCost(SortieDetails) is not SortieCostModel cost) return SortieCostModel.Zero;

		model.CalculatedSortieCost.SortieFleetJetCost = cost;

		db.Sorties.Update(model);
		db.SaveChanges();

		return model.CalculatedSortieCost.SortieFleetJetCost;
	}

	private static SortieCostModel? CalculateFleetJetCost(SortieDetailViewModel sortieDetails)
	{
		IEnumerable<BattleNode> jetPhaseBattles = sortieDetails.Nodes
			.OfType<BattleNode>()
			.Where(b => b.AllPhases.OfType<PhaseJetAirBattle>().Any());

		int steelCost = 0;

		foreach (BattleNode node in jetPhaseBattles)
		{
			List<int>? costs = node.FirstBattle.FleetsBeforeBattle.Fleet.MembersWithoutEscaped?
				.OfType<IShipData>()
				.SelectMany(s => s.AllSlotInstance.Zip(s.Aircraft, (e, a) => (Equipment: e, Aicraft: a)))
				.Select(s => s.Equipment?.MasterEquipment.JetSteelCost(s.Aicraft) ?? 0)
				.ToList();

			if (costs is null) return null;

			steelCost += costs.Sum();
		}

		return new() { Steel = steelCost };
	}

	private SortieCostModel AirBaseJetCost(KCViewerContext db, SortieRecord model)
	{
		if (model.CalculatedSortieCost.AirBaseJetCost is not null)
		{
			return model.CalculatedSortieCost.AirBaseJetCost;
		}

		SortieDetails ??= ToolService.GenerateSortieDetailViewModel(db, model);

		if (SortieDetails is null) return SortieCostModel.Zero;

		model.CalculatedSortieCost.AirBaseJetCost = CalculateAirBaseJetCost(SortieDetails);

		db.Sorties.Update(model);
		db.SaveChanges();

		return model.CalculatedSortieCost.AirBaseJetCost;
	}

	private static SortieCostModel CalculateAirBaseJetCost(SortieDetailViewModel sortieDetails)
	{
		int steelCost = sortieDetails.Nodes
			.OfType<BattleNode>()
			.SelectMany(b => b.AllPhases.OfType<PhaseJetBaseAirAttack>())
			.SelectMany(p => p.Units)
			.SelectMany(u => u.Squadrons)
			.Sum(s => s.Equipment.JetSteelCost(s.AircraftCount));

		return new() { Steel = steelCost };
	}
}
