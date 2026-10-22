using CommunityToolkit.Mvvm.Input;
using KCViewer.Resource;
using KCViewer.Utility;
using KCViewer.ViewModels;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the screenshot panel (mockup 20261009-screenshot-capture-v2; user's 「단축키 대신 패널에 캡처버튼을 원하는
/// 위치에 배치하는것도 지원」, 「1. 가」): one button filling the panel, to dock anywhere, float, or put among other panels'
/// tabs. Closed at first; the panel menu opens it. Whatever the style (with the style off, the viewer's own button). The
/// button is dimmed while the browser is not running; its tooltip names the screenshot key.
/// Added by FormMainViewModel.AddForkViews and shown by the template in Fork/ForkViews.xaml.
/// </summary>
public sealed partial class ForkScreenshotPanelViewModel : AnchorableViewModel
{
	public string ButtonText => ForkStrings.Screenshot;
	public string ButtonToolTip { get; private set; } = "";
	public bool CanTake { get; private set; }

	public ForkScreenshotPanelViewModel() : base(ForkStrings.Screenshot, "Fork.Screenshot", IconContent.BrowserScreenShot)
	{
		SystemEvents.UpdateTimerTick += Refresh;
		Configuration.Instance.ConfigurationChanged += Refresh;
		Refresh();
	}

	[RelayCommand]
	private void Take() => ForkScreenshot.Take();

	private void Refresh()
	{
		CanTake = ForkScreenshot.CanTake;
		ButtonToolTip = ForkStrings.ScreenshotButtonToolTip(ForkHotkey.Parse(ForkSettings.Current.Screenshot.Hotkey)?.ToString() ?? "");
	}
}
