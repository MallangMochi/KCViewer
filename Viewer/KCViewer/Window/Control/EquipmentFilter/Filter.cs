using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;

namespace KCViewer.Window.Control.EquipmentFilter;

public class Filter : ObservableObject
{
	public EquipmentTypeGroup Value { get; set; }
	public string Display => Value.Display();
	public bool IsChecked { get; set; }

	public Filter(EquipmentTypeGroup value)
	{
		Value = value;
	}
}
