using System;
using System.Diagnostics;
using System.Windows;
using KCViewer.Fork.KcvViews;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the help menu's links lead to our repository instead of 74식's (user's decision 2026-10-22 "우리 링크로",
/// mockup 20261022-program-update-v5 ①). As 74식 did, 「온라인 도움말」 and 「문제 보고」 ask before the browser opens
/// (KanColleViewer's small window; the viewer's message box while the style is off) and the release page opens at once.
/// </summary>
public static class ForkHelpLinks
{
	public const string Repository = "https://github.com/MallangMochi/KCViewer";

	/// <summary>The viewer's README (Viewer/ in the repository).</summary>
	public const string HelpUrl = Repository + "/tree/main/Viewer";

	public const string IssuesUrl = Repository + "/issues";

	public const string ReleasesUrl = Repository + "/releases";

	/// <summary>One release's page (its tag, v1.0.0).</summary>
	public static string ReleaseUrl(string version) => $"{ReleasesUrl}/tag/v{version}";

	/// <summary>도움말 › 온라인 도움말 (FormMainViewModel.ViewHelp): always handled.</summary>
	public static bool TryOpenHelp(System.Windows.Window owner)
	{
		if (Ask(owner, ForkStrings.HelpLinkTitle)) Open(HelpUrl);
		return true;
	}

	/// <summary>도움말 › 문제 보고 (FormMainViewModel.ReportIssue): always handled.</summary>
	public static bool TryReportIssue(System.Windows.Window owner)
	{
		if (Ask(owner, ForkStrings.ReportIssueTitle)) Open(IssuesUrl);
		return true;
	}

	private static bool Ask(System.Windows.Window owner, string title)
	{
		if (!KcvViewState.Current.Enabled)
		{
			return MessageBox.Show(owner, ForkStrings.OpenInBrowserQuestion, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes)
				== MessageBoxResult.Yes;
		}

		return KcvUpdateDialog.Ask(owner, title, ForkStrings.OpenInBrowserQuestion, null, [ForkStrings.Yes, ForkStrings.No], 340) == 0;
	}

	/// <summary>A page in the user's browser; a failure goes to the log.</summary>
	public static void Open(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
		}
		catch (Exception e)
		{
			Utility.Logger.Add(3, $"{url}: {e.Message}");
		}
	}
}
