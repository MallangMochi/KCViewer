using KCViewer.Core.Types;

namespace KCViewer.Window.Tools.FleetImageGenerator;

public class CutInShipViewModel : ShipViewModel
{
	public override CutInShipViewModel Initialize(IShipData? ship)
	{
		base.Initialize(ship);

		return this;
	}
}