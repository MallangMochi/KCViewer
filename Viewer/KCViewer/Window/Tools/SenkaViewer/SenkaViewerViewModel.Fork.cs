using System.Linq;

namespace KCViewer.Window.Tools.SenkaViewer;

/// <summary>
/// Fork: the totals as numbers for the cards of the KanColleViewer style window (Fork/KcvViews/KcvSenkaViewerWindow),
/// and telling the viewer's own total texts about new search results (they stayed 0 until a row was selected).
/// </summary>
public partial class SenkaViewerViewModel
{
	public double ForkTotalSenka => TotalCalculationList.Sum(s => s.TotalSenkaGains);
	public double ForkTotalNormalSenka => TotalCalculationList.Sum(s => s.EstimatedHqExpSenkaGains);
	public double ForkTotalExtraOperationSenka => TotalCalculationList.Sum(s => s.ExtraOperationSenkaGains);
	public double ForkTotalQuestSenka => TotalCalculationList.Sum(s => s.QuestSenkaGains);
	public int ForkSessionCount => DataGridViewModel.ItemsSource.Count;

	public void ForkRefreshTotals()
	{
		OnPropertyChanged(nameof(TotalSenka));
		OnPropertyChanged(nameof(TotalNormalSenka));
		OnPropertyChanged(nameof(TotalExtraOperationSenka));
		OnPropertyChanged(nameof(TotalQuestSenka));
		OnPropertyChanged(nameof(ForkTotalSenka));
		OnPropertyChanged(nameof(ForkTotalNormalSenka));
		OnPropertyChanged(nameof(ForkTotalExtraOperationSenka));
		OnPropertyChanged(nameof(ForkTotalQuestSenka));
		OnPropertyChanged(nameof(ForkSessionCount));
	}
}
