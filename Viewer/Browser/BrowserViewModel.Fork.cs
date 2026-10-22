using System.Threading.Tasks;

namespace Browser;

public abstract partial class BrowserViewModel
{
	/// <summary>Fork: called at the end of <see cref="ConfigurationChanged"/>, for the fork's own browser settings.</summary>
	protected virtual Task OnForkConfigurationChangedAsync() => Task.CompletedTask;

	/// <summary>
	/// Fork: the viewer's screenshot key next to the tool menu's 스크린샷 item ("" when there is none). The key itself is
	/// the viewer's (Fork/ForkScreenshot.cs, mockup 20261009-screenshot-capture-v2): the browser has no screenshot key of
	/// its own any more.
	/// </summary>
	public string ForkScreenshotGestureText { get; private set; } = "";

	/// <summary>Fork: a screenshot asked by the viewer (its shortcut key or the screenshot panel).</summary>
	public void TakeScreenshot()
	{
		App.Current.Dispatcher.BeginInvoke(Screenshot);
	}

	/// <summary>Fork: asks the viewer for its screenshot key, at startup and on each configuration change.</summary>
	protected async Task UpdateForkScreenshotHotkeyAsync()
	{
		string text = await BrowserHost.ScreenshotHotkey();
		await App.Current.Dispatcher.InvokeAsync(() => ForkScreenshotGestureText = text);
	}
}
