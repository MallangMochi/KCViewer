namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the battle detail in KanColleViewer's style (see the XAML). As the viewer's, each opening shows the battle at
/// that moment in its own window.
/// </summary>
public partial class KcvBattleDetailWindow
{
	public KcvBattleDetailWindow(KcvBattleDetailViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}
