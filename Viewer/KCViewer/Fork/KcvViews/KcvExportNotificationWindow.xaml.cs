using System.Windows;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the sortie record's failure notice in KanColleViewer's style (see the XAML).</summary>
public partial class KcvExportNotificationWindow
{
	public KcvExportNotificationWindow(string text, string title)
	{
		InitializeComponent();

		Title = title;
		MessageText.Text = text;
	}

	private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
