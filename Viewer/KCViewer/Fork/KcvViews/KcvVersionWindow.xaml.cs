using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Navigation;
using KCViewer.Utility;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: 버전 정보 in KanColleViewer's style (see the XAML), opened through <see cref="KcvMenuWindows.TryShowVersion"/>.
/// The values are the viewer's window's (Window/Dialog/VersionInformation) without their 「이름: 」.
/// </summary>
public partial class KcvVersionWindow
{
	public KcvVersionWindow()
	{
		InitializeComponent();

		VersionText.Text = ForkStrings.VersionValue(ForkVersion.ReleaseVersion, ForkVersion.CommitText, ForkVersion.CommitDate);
		BuiltText.Text = SoftwareInformation.UpdateTime.ToString("yyyy-MM-dd HH:mm");
		BaseText.Text = ForkStrings.VersionBaseValue(
			CultureInfo.CurrentUICulture.Name.StartsWith("ja") ? SoftwareInformation.SoftwareNameJapanese : SoftwareInformation.SoftwareNameEnglish,
			SoftwareInformation.VersionEnglish);
		RuntimeText.Text = RuntimeInformation.FrameworkDescription;

		// the viewer's icon at 32 px; no picture without one
		Logo.Source = KcvMenuWindows.AppIcon(32);
		if (Logo.Source is null) Logo.Visibility = Visibility.Collapsed;
	}

	private void OpenLink(object sender, RequestNavigateEventArgs e)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = e.Uri.AbsoluteUri,
			UseShellExecute = true,
		});
	}

	private void CloseWindow(object sender, RoutedEventArgs e) => Close();
}
