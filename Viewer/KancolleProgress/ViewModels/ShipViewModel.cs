using KCViewer.Core.Types;

namespace KancolleProgress.ViewModels;

public class ShipViewModel
{
	public IShipData Ship { get; }
	public string Name => Ship.Name;
	public int Level => Ship.Level;

	public ShipViewModel(IShipData ship)
	{
		Ship = ship;
	}
}
