namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: 칸무스 수집 진행도 in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/> while the style is on.</summary>
public partial class KcvKancolleProgressWindow
{
	public KcvKancolleProgressWindow(KcvKancolleProgressViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}
