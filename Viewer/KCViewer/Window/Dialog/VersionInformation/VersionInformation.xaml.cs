using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using KCViewer.Utility;

namespace KCViewer.Window.Dialog.VersionInformation;
/// <summary>
/// Interaction logic for VersionInformation.xaml
/// </summary>
public partial class VersionInformationWindow
{
	public VersionInformationWindow()
	{
		InitializeComponent();

		Translate();
		string versionText = CultureInfo.DefaultThreadCurrentCulture?.Name switch
		{
			"ja-JP" => SoftwareInformation.VersionJapanese,
			_ => SoftwareInformation.SoftwareNameEnglish
		};

		TextVersion.Text = string.Format(VersionResources.TextVersionFormat, versionText, SoftwareInformation.VersionEnglish, SoftwareInformation.UpdateTime.ToString("yyyy/MM/dd"));
		RuntimeVersion.Text = RuntimeInformation.FrameworkDescription;

		// Fork: the fork's version first and the viewer as its base ("버전 정보는 우리 버전으로 변경", 2026-10-05). The
		// upstream project's links (bug reports, Discord, Twitter) are for the viewer itself, so they are hidden.
		TextVersion.Text = Fork.ForkStrings.ProductDisplayName;
		ForkCommitText.Text = Fork.ForkStrings.VersionCommit(Fork.ForkVersion.ReleaseVersion, Fork.ForkVersion.CommitText, Fork.ForkVersion.CommitDate);
		ForkBuiltText.Text = Fork.ForkStrings.VersionBuilt(SoftwareInformation.UpdateTime.ToString("yyyy-MM-dd HH:mm"));
		ForkBaseText.Text = Fork.ForkStrings.VersionBase(
			CultureInfo.CurrentUICulture.Name.StartsWith("ja") ? SoftwareInformation.SoftwareNameJapanese : SoftwareInformation.SoftwareNameEnglish,
			SoftwareInformation.VersionEnglish);
		TextAuthor.Text = Fork.ForkStrings.VersionOriginalDeveloper;
		TextModifiedBy.Text = Fork.ForkStrings.VersionOriginalMaintainers;
		ProjectSiteLine.Visibility = Visibility.Collapsed;

		// https://github.com/Kinnara/ModernWpf/issues/378
		SourceInitialized += (s, a) =>
		{
			Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Input);
		};
	}

	private void Translate()
	{
		TextVersion.Text = VersionResources.TextVersion;
		TextAuthor.Text = VersionResources.Developer;
		TextAuthorLink.Inlines.Add(VersionResources.TextAuthor);
		ButtonClose.Content = VersionResources.ButtonClose;
		TextProjectSite.Text = VersionResources.ProjectSite;
		TextModifiedBy.Text = VersionResources.ModifiedBy;
		TextMaintainers.Text = VersionResources.Maintainers;
		Title = VersionResources.Title;
	}

	private void OpenHyperlink(object sender, RequestNavigateEventArgs e)
	{
		ProcessStartInfo psi = new()
		{
			FileName = e.Uri.AbsoluteUri,
			UseShellExecute = true
		};
		Process.Start(psi);
	}

	private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
