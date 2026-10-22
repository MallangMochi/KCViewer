using System.Windows;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the sortie record's 추출 필터 in KanColleViewer's style (see the XAML); OK is true, 취소 and Esc are not.</summary>
public partial class KcvExportFilterWindow
{
	public KcvExportFilterWindow(KcvExportFilterViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}

	private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
