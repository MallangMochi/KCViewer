using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;

namespace KCViewer.Avalonia.Controls.EquipmentFilter;

public partial class Filter(EquipmentTypeGroup value) : ObservableObject
{
	public EquipmentTypeGroup Value { get; } = value;
	public string Display => Value.Display();
	[ObservableProperty] public partial bool IsChecked { get; set; }
}
