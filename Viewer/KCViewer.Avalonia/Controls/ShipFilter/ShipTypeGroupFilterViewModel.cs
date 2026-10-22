using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;

namespace KCViewer.Avalonia.Controls.ShipFilter;

public partial class ShipTypeGroupFilterViewModel(ShipTypeGroup value) : ObservableObject
{
	public ShipTypeGroup Value { get; } = value;
	public string Display => Value.Display();
	[ObservableProperty] public partial bool IsChecked { get; set; }
}
