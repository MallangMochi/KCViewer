namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the sortie record's 출격 상세 in KanColleViewer's style (see the XAML), opened for each sortie as the viewer's.
/// </summary>
public partial class KcvSortieDetailWindow
{
	public KcvSortieDetailWindow(KcvSortieDetailViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}
