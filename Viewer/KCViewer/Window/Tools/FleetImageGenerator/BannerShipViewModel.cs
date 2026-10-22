using KCViewer.Core.Types;

namespace KCViewer.Window.Tools.FleetImageGenerator;

public class BannerShipViewModel : ShipViewModel
{
	public override BannerShipViewModel Initialize(IShipData? ship)
	{
		base.Initialize(ship);

		return this;
	}
}