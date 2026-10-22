namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: 출격 코스트 in KanColleViewer's style (see the XAML), from the sortie record's row menu while the style is on.</summary>
public partial class KcvSortieCostWindow
{
	private KcvSortieRoutes Routes { get; }

	public KcvSortieCostWindow(KcvSortieCostViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		// the routes of the sorties, read in the background as the sortie record does
		Routes = (KcvSortieRoutes)Resources["Routes"];
		Routes.Load(viewModel.Sorties);
		Closed += (_, _) => Routes.Cancel();
	}
}
