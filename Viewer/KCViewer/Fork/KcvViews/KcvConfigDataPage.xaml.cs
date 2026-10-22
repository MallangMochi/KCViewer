using System.Windows;
using System.Windows.Controls;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: 데이터 of the settings window in KanColleViewer's style (see the XAML).</summary>
public partial class KcvConfigDataPage
{
	public KcvConfigDataPage()
	{
		InitializeComponent();
	}

	/// <summary>The login takes the password from the box, as the viewer's page passes it.</summary>
	private void PasswordChanged(object sender, RoutedEventArgs e)
	{
		LoginButton.CommandParameter = (sender as PasswordBox)?.SecurePassword;
	}
}
