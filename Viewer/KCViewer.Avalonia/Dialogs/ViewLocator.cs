using KCViewer.Avalonia.Dialogs.EquipmentSelector;
using KCViewer.Avalonia.Dialogs.ShipSelector;
using HanumanInstitute.MvvmDialogs.Avalonia;

namespace KCViewer.Avalonia.Dialogs;

public class ViewLocator : StrongViewLocator
{
	public ViewLocator()
	{
		Register<ShipSelectorViewModel, ShipSelectorView, ShipSelectorWindow>();
		Register<EquipmentSelectorViewModel, EquipmentSelectorView, EquipmentSelectorWindow>();
	}
}
