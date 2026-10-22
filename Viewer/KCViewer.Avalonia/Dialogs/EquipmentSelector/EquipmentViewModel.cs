using KCViewer.Core.Types;

namespace KCViewer.Avalonia.Dialogs.EquipmentSelector;

public class EquipmentViewModel(IEquipmentData equipment)
{
	public IEquipmentData Equipment { get; } = equipment;
}
