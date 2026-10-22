using System.Collections.Generic;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Window.Tools.EquipmentUpgradePlanner.CostCalculation;

namespace KCViewer.Window.Tools.EquipmentUpgradePlanner;

public interface IEquipmentPlanItemViewModel
{
	public EquipmentId EquipmentMasterDataId { get; }

	public IEquipmentDataMaster? EquipmentMasterData => KCDatabase.Instance.MasterEquipments.ContainsKey((int)EquipmentMasterDataId) switch
	{
		true => KCDatabase.Instance.MasterEquipments[(int)EquipmentMasterDataId],
		_ => null,
	};

	public EquipmentUpgradePlanCostViewModel Cost { get; }

	public void UnsubscribeFromApis();

	public List<IEquipmentPlanItemViewModel> GetPlanChildren();
}
