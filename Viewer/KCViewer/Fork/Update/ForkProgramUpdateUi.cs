using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using KCViewer.Fork.KcvViews;

namespace KCViewer.Fork.Update;

/// <summary>
/// Fork: the screens of the viewer's own program updates (mockup 20261022-program-update-v5, approved 2026-10-22
/// "5판대로 release/Viewer에서 구현해"), over <see cref="ForkProgramUpdate"/>:
/// <list type="bullet">
/// <item>the menu bar's update item (②~⑦) in place of 74식's 「업데이트 데이터가 존재합니다」, hidden while there is nothing
/// to say;</item>
/// <item>the window when a release is downloaded and checked (⑧), the manual install notice once a version (⑨);</item>
/// <item>「지금 다시 시작」 and 「종료 시 업데이트 예약」: the updater starts once the viewer has closed
/// (FormMainViewModel.Closed), restarting it or not.</item>
/// </list>
/// Only the live instance (2026-10-19, 13번); the help menu's names are ForkMenuLayout's.
/// </summary>
public static class ForkProgramUpdateUi
{
	/// <summary>The binding path of 74식's update item's header in FormMainWpf.xaml.</summary>
	private const string UpdateItemHeader = "FormMain.StripMenu_Update_UpdateAvailable";

	/// <summary>The item's colour while something waits for the user (②, ④, ⑤), as on the mockup.</summary>
	private static readonly Brush Highlight = Frozen(Color.FromArgb(0x38, 0xE8, 0xA1, 0x3A));

	private static System.Windows.Window? Window { get; set; }

	private static MenuItem? Item { get; set; }

	/// <summary>
	/// ③'s line while the item shows the download: then only its text and the header change. Clearing the items closes
	/// an open submenu, so building them again at every half percent closed the menu under the user (2026-10-22 test).
	/// </summary>
	private static TextBlock? ProgressLine { get; set; }

	/// <summary>⑧ comes once a session, when the download is checked.</summary>
	private static bool ReadyAsked { get; set; }

	/// <summary>「종료 시 업데이트 예약」: the updater runs when the viewer closes, and the viewer does not start again.</summary>
	public static bool UpdateAtExit { get; private set; }

	/// <summary>「지금 다시 시작」 is closing the viewer: its closing question is skipped (FormMainViewModel.Closing).</summary>
	public static bool RestartingForUpdate { get; private set; }

	/// <summary>Called at start (FormMainViewModel.AddForkViews).</summary>
	public static void Install(System.Windows.Window window)
	{
		// 74식's item, whatever the instance: its condition (74식's bld_date) is no longer ours
		if (ForkMenuLayout.FindMenu(window, UpdateItemHeader) is { } item)
		{
			BindingOperations.ClearBinding(item, HeaderedItemsControl.HeaderProperty);
			BindingOperations.ClearBinding(item, UIElement.VisibilityProperty);
			item.Items.Clear();
			item.Visibility = Visibility.Collapsed;
			Item = item;
		}

		if (ForkProfile.Name is not null || ForkProfile.IsEmbedded) return;

		Window = window;
		ForkProgramUpdate.Changed += () => window.Dispatcher.InvokeAsync(Refresh);
		window.ContentRendered += OnShown;

		void OnShown(object? sender, EventArgs e)
		{
			window.ContentRendered -= OnShown;
			ForkProgramUpdate.Start();
		}
	}

	/// <summary>도움말 › 업데이트 확인, after the translations and data (FormMainViewModel.CheckForUpdate).</summary>
	public static Task CheckFromMenuAsync() => ForkProgramUpdate.CheckAsync(manual: true);

	/// <summary>The viewer has closed (FormMainViewModel.Closed): the updater, when one was chosen.</summary>
	public static void OnViewerClosed()
	{
		if (RestartingForUpdate) ForkProgramUpdate.StartUpdater(restart: true);
		else if (UpdateAtExit) ForkProgramUpdate.StartUpdater(restart: false);
	}

	private static void Refresh()
	{
		ShowItem();

		switch (ForkProgramUpdate.State)
		{
			case ProgramUpdateState.Ready when !ReadyAsked:
				ReadyAsked = true;
				AskReady();
				break;
			case ProgramUpdateState.ManualInstall:
				NoticeManualInstall();
				break;
		}
	}

	/// <summary>② ~ ⑦; hidden while not looked yet, looking, or up to date.</summary>
	private static void ShowItem()
	{
		if (Item is not { } item) return;

		RemoteRelease? latest = ForkProgramUpdate.Latest;
		string version = latest?.Version ?? "";
		string releasePage = latest is null ? ForkHelpLinks.ReleasesUrl : ForkHelpLinks.ReleaseUrl(version);

		if (ForkProgramUpdate.State == ProgramUpdateState.Downloading && latest is not null && ProgressLine is { } line)
		{
			ShowProgress(item, line, latest);
			return;
		}

		item.Items.Clear();
		ProgressLine = null;
		item.ClearValue(Control.BackgroundProperty);
		item.Visibility = Visibility.Visible;

		switch (ForkProgramUpdate.State)
		{
			case ProgramUpdateState.Available when latest is not null:
				item.Header = version;
				item.Background = Highlight;
				Info(item, ForkStrings.UpdateFromTo(ForkVersion.ReleaseVersion ?? "", version));
				if (latest.Note.Trim().Length > 0) Info(item, latest.Note.Trim());
				item.Items.Add(new Separator());
				AddAction(item, ForkStrings.UpdateDownload(latest.Package.Size), () => _ = ForkProgramUpdate.DownloadAsync());
				break;

			case ProgramUpdateState.Downloading when latest is not null:
				ProgressLine = Info(item, "");
				ShowProgress(item, ProgressLine, latest);
				item.Items.Add(new Separator());
				break;

			case ProgramUpdateState.Ready:
				item.Header = ForkStrings.UpdateReady(version);
				item.Background = Highlight;
				AddAction(item, ForkStrings.RestartNow, RestartNow);
				MenuItem atExit = AddAction(item, ForkStrings.UpdateAtExit, () => UpdateAtExit = !UpdateAtExit);
				atExit.IsCheckable = true;
				atExit.IsChecked = UpdateAtExit;
				item.Items.Add(new Separator());
				break;

			case ProgramUpdateState.ManualInstall:
				item.Header = ForkStrings.UpdateManualInstall(version);
				item.Background = Highlight;
				Info(item, ForkProgramUpdate.ManualReason == ManualInstallReason.NoInstallList ? ForkStrings.NoInstallListLine : ForkStrings.NewerUpdaterLine);
				item.Items.Add(new Separator());
				break;

			case ProgramUpdateState.DevelopmentBuild:
				item.Header = ForkStrings.UpdateRelease(version);
				Info(item, ForkStrings.DevelopmentBuildLine);
				item.Items.Add(new Separator());
				break;

			case ProgramUpdateState.Failed:
				bool download = ForkProgramUpdate.FailedDuring == ProgramUpdateFailure.Download && latest is not null;
				item.Header = ForkStrings.UpdateFailed;
				Info(item, download ? ForkStrings.DownloadFailedLine(version) : ForkStrings.CheckFailedLine);
				item.Items.Add(new Separator());
				AddAction(item, ForkStrings.Retry, () => _ = ForkProgramUpdate.RetryAsync());
				if (!download) releasePage = ForkHelpLinks.ReleasesUrl;
				break;

			default:
				item.Visibility = Visibility.Collapsed;
				return;
		}

		AddAction(item, ForkStrings.OpenReleasePage, () => ForkHelpLinks.Open(releasePage));
	}

	/// <summary>③'s header and line: how far the download is.</summary>
	private static void ShowProgress(MenuItem item, TextBlock line, RemoteRelease latest)
	{
		long size = Math.Max(1, latest.Package.Size);
		item.Header = ForkStrings.UpdateDownloading(latest.Version, (int)(ForkProgramUpdate.Downloaded * 100 / size));
		line.Text = ForkStrings.UpdateDownloadedOf(latest.Version, ForkProgramUpdate.Downloaded, latest.Package.Size);
	}

	/// <summary>
	/// A grey line that cannot be clicked: what the state means. The text block keeps its own colour when its item is
	/// off, so it takes the panels' dim text colour itself (it stayed white on the first test, 2026-10-22).
	/// </summary>
	private static TextBlock Info(MenuItem parent, string text)
	{
		TextBlock line = new() { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 300 };
		line.SetResourceReference(TextBlock.ForegroundProperty, "Kcv.InactiveForeground");
		parent.Items.Add(new MenuItem { Header = line, IsEnabled = false });
		return line;
	}

	private static MenuItem AddAction(MenuItem parent, string header, Action action)
	{
		MenuItem item = new() { Header = header };
		item.Click += (_, _) => action();
		parent.Items.Add(item);
		return item;
	}

	/// <summary>⑧: 지금 다시 시작 / 종료 시 업데이트 예약 / 업데이트 안 함 (nothing chosen; the menu bar item stays).</summary>
	private static void AskReady()
	{
		if (ForkProgramUpdate.Latest is not { } latest) return;

		int choice = KcvUpdateDialog.Ask(Window, ForkStrings.ProgramUpdateTitle, ForkStrings.ReadyQuestion(latest.Version), latest.Note,
			[ForkStrings.RestartNowButton, ForkStrings.UpdateAtExitButton, ForkStrings.NoUpdateButton]);

		switch (choice)
		{
			case 0:
				RestartNow();
				break;
			case 1:
				UpdateAtExit = true;
				ShowItem();
				break;
		}
	}

	/// <summary>⑨ once a version; after that the menu bar item (⑤) says it.</summary>
	private static void NoticeManualInstall()
	{
		if (ForkProgramUpdate.Latest is not { } latest) return;

		ForkSettings.ProgramUpdateSettings settings = ForkSettings.Current.ProgramUpdate;
		if (settings.ManualNoticeVersion == latest.Version) return;

		settings.ManualNoticeVersion = latest.Version;
		ForkSettings.Current.Save();

		int choice = KcvUpdateDialog.Ask(Window, ForkStrings.ProgramUpdateTitle,
			ForkStrings.ManualInstallMessage(latest.Version, ForkProgramUpdate.ManualReason), null,
			[ForkStrings.OpenReleasePageButton, ForkStrings.CloseButton]);

		if (choice == 0) ForkHelpLinks.Open(ForkHelpLinks.ReleaseUrl(latest.Version));
	}

	/// <summary>Closes the viewer without its closing question; the updater starts once it has closed and starts it again.</summary>
	private static void RestartNow()
	{
		if (Window is not { } window || ForkProgramUpdate.State != ProgramUpdateState.Ready) return;

		RestartingForUpdate = true;
		window.Close();

		// still open: something else kept it (a plugin, a dialog), so nothing is done
		if (window.IsVisible) RestartingForUpdate = false;
	}

	private static Brush Frozen(Color color)
	{
		SolidColorBrush brush = new(color);
		brush.Freeze();
		return brush;
	}
}
