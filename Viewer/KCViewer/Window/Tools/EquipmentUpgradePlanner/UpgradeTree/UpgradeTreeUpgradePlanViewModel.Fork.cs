using KCViewer.Core.Types;
using KCViewer.Fork;
using KCViewer.Fork.KcvViews;
using KCViewer.Window.Tools.EquipmentUpgradePlanner.EquipmentAssignment;

namespace KCViewer.Window.Tools.EquipmentUpgradePlanner.UpgradeTree;

/// <summary>
/// Fork: what the upgrade tree in KanColleViewer's style (KcvUpgradeTreeWindow, mockup 20261009-upgrade-plan-windows-v2)
/// shows of an item instead of the viewer's one line: its kind, count, name, icon and the mark beside the name.
/// </summary>
public partial class UpgradeTreeUpgradePlanViewModel
{
	public bool ForkIsPlan => Plan is EquipmentUpgradePlanItemViewModel;
	public bool ForkIsCraft => Plan is EquipmentCraftPlanItemViewModel;

	/// <summary>「×5」 for the items got by remodelling, developed or assigned; nothing for a plan (always one).</summary>
	public string ForkCount => ForkIsPlan ? "" : $"×{Count}";

	public string ForkName => Plan switch
	{
		EquipmentUpgradePlanItemViewModel plan => plan.EquipmentName,
		_ => Plan.EquipmentMasterData?.NameEN ?? "",
	};

	public EquipmentIconType? ForkIcon => Plan.EquipmentMasterData?.IconTypeTyped;

	/// <summary>
	/// The mark beside the name: a plan's goal (「목표」 with <see cref="ForkGoal"/>, or 「개수 갱신」 for one that remodels),
	/// 「개발·입수」, 「할당됨」. The items got by remodelling have none (user's decision 2026-10-09 "갱신해서 마련 은 제외").
	/// </summary>
	public string ForkMark => Plan switch
	{
		EquipmentUpgradePlanItemViewModel { DesiredUpgradeLevel: UpgradeLevel.Conversion } plan => KcvUpgradeLevelConverter.Text(plan.DesiredUpgradeLevel),
		EquipmentUpgradePlanItemViewModel => ForkStrings.KcvUpgradeTreeGoal,
		EquipmentCraftPlanItemViewModel => ForkStrings.KcvUpgradeTreeCraft,
		EquipmentAssignmentItemViewModel => ForkStrings.KcvUpgradeTreeOwned,
		_ => "",
	};

	/// <summary>
	/// A plan's goal after 「목표」 (「 ★max」), shown in the improvement colour of <see cref="ForkGoalLevel"/> as the
	/// planner's cards (user's remark 2026-10-09 "개수 맥스는 원래 개수치 색상대로"); nothing otherwise.
	/// </summary>
	public string ForkGoal => ForkGoalLevel is { } level ? $" {KcvUpgradeLevelConverter.Text(level)}" : "";

	public UpgradeLevel? ForkGoalLevel => Plan is EquipmentUpgradePlanItemViewModel { DesiredUpgradeLevel: not UpgradeLevel.Conversion } plan
		? plan.DesiredUpgradeLevel
		: null;

	/// <summary>For the mark's colour: plan, craft, owned or none.</summary>
	public string ForkMarkKind => Plan switch
	{
		EquipmentUpgradePlanItemViewModel => "Plan",
		EquipmentCraftPlanItemViewModel => "Craft",
		EquipmentAssignmentItemViewModel => "Owned",
		_ => "",
	};
}
