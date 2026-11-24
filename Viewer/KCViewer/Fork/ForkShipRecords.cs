using KCViewer.Data;
using KCViewer.Resource.Record;
using KCViewer.Utility;

namespace KCViewer.Fork;

/// <summary>
/// Fork: repairs of the ship parameter record the viewer makes by itself (RECORD-DATA-DESIGN.md).
/// </summary>
public static class ForkShipRecords
{
	/// <summary>
	/// Clears the costume link (OriginalCostumeShipID) of a record that is a real ship when it points to a ship of another
	/// remodel line, and returns how many it cleared (RECORD-DATA-DESIGN.md, section 4, user's decision 2026-11-24).
	/// <para>
	/// The game gives numbers it once used for costume pictures to new ships (750 由良改二偵 had been a costume of 翔鶴 in the
	/// default record, 166 others before it), and the image viewer shows every record linked to a ship as its costume. A
	/// link within one line (長良改 to 長良, set from the picture book) stays, and so does a costume picture's own number
	/// (no master ship). A link cleared by mistake comes back when the picture book is opened.
	/// </para>
	/// Called at the game start, after the master data and the remodel links (RemodelBeforeShipID) are read.
	/// </summary>
	public static int ClearStaleCostumeLinks(ShipParameterRecord record)
	{
		var ships = KCDatabase.Instance.MasterShips;
		int cleared = 0;

		foreach (ShipParameterRecord.ShipParameterElement element in record.Record.Values)
		{
			if (element.OriginalCostumeShipID == -1) continue;
			if (ships[element.ShipID] is not { IsAbyssalShip: false } ship) continue;
			if (ships[element.OriginalCostumeShipID] is { } owner && owner.BaseShip().ShipID == ship.BaseShip().ShipID) continue;

			element.OriginalCostumeShipID = -1;
			cleared++;
		}

		if (cleared > 0)
		{
			Logger.Add(2, ForkStrings.ShipRecordStaleCostumeLinksCleared(cleared));
		}

		return cleared;
	}
}
