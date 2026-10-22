using CsvHelper.Configuration;

namespace KCViewer.Window.Tools.SortieRecordViewer.DataExport;

public sealed class BattleRanksExportMap : ClassMap<BattleRanksExportModel>
{
	public BattleRanksExportMap()
	{
		Map(m => m.Date).Name(CsvExportResources.Date);
		Map(m => m.World).Name(CsvExportResources.World);
		Map(m => m.Square).Name(CsvExportResources.Square);
		Map(m => m.ExpectedRank).Name(CsvExportResources.ExpectedRank);
		Map(m => m.ActualRank).Name(CsvExportResources.ActualRank);
	}
}
