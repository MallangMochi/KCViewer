using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using KCViewer.Core.Types;
using KCViewer.Fork;

namespace KCViewer.Window.Wpf.ShipTrainingPlanner;

/// <summary>A remodel in the KanColleViewer-style editor's list: the viewer's item and its text (「나가토 改2 (Lv.88)」).</summary>
public sealed record KcvTrainingRemodel(ComboBoxShip Ship, string Text);

/// <summary>
/// Fork: what the plan editor in KanColleViewer's style (KcvShipTrainingPlanWindow, mockup 20261008-ship-training-edit-v2)
/// shows beside the viewer's values: whether each goal is reached, the limits of its number boxes, and the remodels with
/// the level each one needs.
/// </summary>
public partial class ShipTrainingPlanViewModel
{
	private static readonly string[] ForkProperties =
	[
		nameof(ForkLevelDone), nameof(ForkExpLeft),
		nameof(ForkHPDone), nameof(ForkHPMax), nameof(ForkHPLeft),
		nameof(ForkASWModernizable), nameof(ForkASWDone), nameof(ForkASWMax), nameof(ForkASWLeft),
		nameof(ForkLuckDone), nameof(ForkLuckMax), nameof(ForkLuckLeft),
		nameof(ForkRemodelReady), nameof(ForkLastRemodel),
	];

	public bool ForkLevelDone => Ship.Level >= TargetLevel;
	public string ForkExpLeft => ForkStrings.KcvTrainingExpLeft(RemainingExpToTarget);
	public string ForkLevelMax => ForkStrings.KcvTrainingMax($"{MaximumLevel}");

	public bool ForkHPDone => Ship.HPMax >= TargetHP;
	public string ForkHPMax => ForkStrings.KcvTrainingMax($"+{MaximumHPMod}");
	public string ForkHPLeft => $"+{RemainingHP}";

	public bool ForkASWModernizable => Ship.MasterShip.ASWModernizable > 0;
	public bool ForkASWDone => Ship.ASWBase >= TargetASW;
	public string ForkASWMax => ForkASWModernizable
		? ForkStrings.KcvTrainingMax($"+{Ship.MasterShip.ASWModernizable}")
		: ForkStrings.KcvTrainingNoModernize;
	public string ForkASWLeft => $"+{RemainingASW}";

	public bool ForkLuckDone => Ship.LuckBase >= TargetLuck;
	public string ForkLuckMax => ForkStrings.KcvTrainingMax($"{Ship.MasterShip.LuckMax}");
	public string ForkLuckLeft => $"+{RemainingLuck}";

	/// <summary>The remodels of <see cref="PossibleRemodels"/>; each after the ship's own form with the level it needs.</summary>
	public List<KcvTrainingRemodel> ForkRemodels =>
	[
		.. PossibleRemodels.Select((remodel, i) => new KcvTrainingRemodel(remodel,
			i > 0 && remodel.Ship.RemodelBeforeShip is { } before
				? $"{remodel.NameEN} (Lv.{before.RemodelAfterLevel})"
				: remodel.NameEN)),
	];

	/// <summary>The ship's level reaches the chosen remodel's (the viewer's check for its remodel alarm).</summary>
	public bool ForkRemodelReady => ShipRemodelLevelReached;

	/// <summary>The ship has no remodel after its form.</summary>
	public bool ForkLastRemodel => Ship.MasterShip.RemodelAfterShip is null;

	/// <summary>
	/// The viewer raises <see cref="Ship"/> by hand when the ship's data comes (and the goals through their setters), so
	/// the values above follow both.
	/// </summary>
	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.PropertyName is not (nameof(Ship) or nameof(TargetLevel) or nameof(TargetHPBonus) or nameof(TargetASWBonus)
			or nameof(TargetLuck) or nameof(TargetRemodel) or nameof(RemainingExpToTarget)))
		{
			return;
		}

		foreach (string name in ForkProperties)
		{
			base.OnPropertyChanged(new PropertyChangedEventArgs(name));
		}
	}
}
