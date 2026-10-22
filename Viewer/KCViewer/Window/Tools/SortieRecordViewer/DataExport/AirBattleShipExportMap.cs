using CsvHelper.Configuration;

namespace KCViewer.Window.Tools.SortieRecordViewer.DataExport;

public sealed class AirBattleShipExportMap : ClassMap<AirBattleShipExportModel>
{
	public AirBattleShipExportMap()
	{
		Map(m => m.Id).Name(CsvExportResources.Id);
		Map(m => m.Name).Name(CsvExportResources.Name);
		Map(m => m.Level).Name(CsvExportResources.ShipLevel);
	}
}
