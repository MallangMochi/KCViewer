namespace KCViewer.Core.Types.Extensions;

public static class ShipIdExtensions
{
	public static bool IsNightCarrier(this ShipId shipId) =>
		shipId is
			ShipId.SaratogaMkII or
			ShipId.AkagiKaiNiE or
			ShipId.KagaKaiNiE or
			ShipId.ShimaneMaruKai or
			ShipId.RyuuhouKaiNiE;

	// Fork: the carriers that shell at night without night aircraft, kept here so the ship (ShipDataExtensions) and the
	// older ship ID based night attack (Calculator.GetNightAttackKind) read one list; the older one had only the first 3
	public static bool IsSpecialNightCarrier(this ShipId shipId) =>
		shipId is
			ShipId.GrafZeppelin or
			ShipId.GrafZeppelinKai or
			ShipId.Saratoga or
			ShipId.TaiyouKaiNi or
			ShipId.ShinyouKaiNi or
			ShipId.UnyouKaiNi or
			ShipId.KagaKaiNiGo or
			ShipId.Lexington or
			ShipId.LexingtonKai;
}
