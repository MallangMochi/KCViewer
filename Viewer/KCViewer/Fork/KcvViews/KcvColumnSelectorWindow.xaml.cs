using System.Windows;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: 열 표시 설정 in KanColleViewer's style (see the XAML), opened through <see cref="KcvTableDialogs.TryShowColumnSelector"/>.</summary>
public partial class KcvColumnSelectorWindow
{
	public KcvColumnSelectorWindow(KcvColumnSelectorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
		// set here: with the width bound in the XAML the window opened as wide as the screen
		Width = viewModel.WindowWidth;
	}

	private void OnConfirmClick(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}
}
