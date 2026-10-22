using System;
using System.Collections.Generic;
using System.Linq;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Serialization.EquipmentUpgrade;

namespace KCViewer.Utility.Data;
public static class EquipmentUpgradeExtensions
{
	public static List<EquipmentUpgradeDataModel> CanUpgradeEquipments(this IShipDataMaster ship, DayOfWeek day, List<EquipmentUpgradeDataModel> upgradesData)
		=> upgradesData.Where(upgrade => upgrade.Improvement
				.Any(improvement => improvement.Helpers
					.Any(helpers => helpers.ShipIds.Contains(ship.ShipID) && helpers.CanHelpOnDays.Contains(day))
				)
			)
			.ToList();
}
