using System.Windows;
using System.Windows.Controls;

namespace KCViewer.Window.Settings.DataSubmission;

public partial class ConfigurationDataSubmissionUserControl
{
	public ConfigurationDataSubmissionUserControl()
	{
		InitializeComponent();
	}

	private void BonoderePasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		BonodereLoginButton.CommandParameter = (sender as PasswordBox)?.SecurePassword;
	}
}
