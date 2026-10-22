using CommunityToolkit.Mvvm.Input;
using KCViewer.Fork;

namespace KCViewer.Window.Settings.SubWindow.Browser;

/// <summary>
/// Fork: the screenshot key (<see cref="ForkSettings.ScreenshotSettings"/>) in 보조 윈도우 › 브라우저 › 스크린샷, where the
/// removed 모드 was (mockup 20261009-screenshot-capture-v2), in both settings windows: the key box and [지우기].
/// </summary>
public partial class ConfigurationBrowserViewModel
{
	/// <summary>The key as the menus show it, "" for none.</summary>
	public string ForkScreenshotHotkey { get; set; } = ForkHotkey.Parse(ForkSettings.Current.Screenshot.Hotkey)?.ToString() ?? "";

	[RelayCommand]
	private void ForkClearScreenshotHotkey() => ForkScreenshotHotkey = "";

	private void ForkSave()
	{
		ForkSettings.ScreenshotSettings screenshot = ForkSettings.Current.Screenshot;
		if (screenshot.Hotkey == ForkScreenshotHotkey) return;

		screenshot.Hotkey = ForkScreenshotHotkey;
		ForkSettings.Current.Save();
	}
}
