using System.Collections.Generic;
using KCViewer.Common;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Window.Tools.EquipmentUpgradePlanner.CostCalculation;
using KCViewer.Core.Types.Mocks;

namespace KCViewer.Window.Tools.EquipmentUpgradePlanner;

public class EquipmentCraftPlanItemViewModel(EquipmentId equipmentId, EquipmentUpgradePlanItemViewModel parentPlan) : WindowViewModelBase, IEquipmentPlanItemViewModel
{
	public EquipmentUpgradePlanItemViewModel PlannedToBeUsedBy { get; set; } = parentPlan;

	public EquipmentId EquipmentMasterDataId { get; set; } = equipmentId;

	public IEquipmentData? Equipment => KCDatabase.Instance.MasterEquipments.ContainsKey((int)EquipmentMasterDataId) switch
	{
		true => new EquipmentDataMock(KCDatabase.Instance.MasterEquipments[(int)EquipmentMasterDataId]),
		_ => null,
	};

	public int RequiredCount { get; set; }

	public EquipmentUpgradePlanCostViewModel Cost { get; set; } = new(new());

	public List<IEquipmentPlanItemViewModel> GetPlanChildren()
	{
		return new();
	}

	public void UnsubscribeFromApis()
	{
		Cost.UnsubscribeFromApis();
	}
}
