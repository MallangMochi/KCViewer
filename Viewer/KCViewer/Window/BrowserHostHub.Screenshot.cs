using System.Threading.Tasks;
using KCViewer.Fork;

namespace KCViewer.Window;

/// <summary>Fork: the viewer's screenshot key for the browser's tool menu (Fork/ForkScreenshot.cs).</summary>
public partial class BrowserHostHub
{
	public Task<string> ScreenshotHotkey()
	{
		return Task.FromResult(ForkHotkey.Parse(ForkSettings.Current.Screenshot.Hotkey)?.ToString() ?? "");
	}
}
