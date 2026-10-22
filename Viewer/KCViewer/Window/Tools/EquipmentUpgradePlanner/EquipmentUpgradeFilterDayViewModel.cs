using System;
using KCViewer.Common;
using KCViewer.Utility;

namespace KCViewer.Window.Tools.EquipmentUpgradePlanner;

public class EquipmentUpgradeFilterDayViewModel : CheckBoxEnumViewModel
{
	public EquipmentUpgradeFilterDayViewModel(Enum value) : base(value)
	{
		Configuration.Instance.ConfigurationChanged += () => OnPropertyChanged("");
	}

	public string DisplayValue => ((DayOfWeek)Value).ToDisplay();
}
