using System.Threading.Tasks;
using KCViewer.Fork;

namespace KCViewer.Window;

/// <summary>Fork: the KanColleViewer style's accent for the browser process (Fork/KcvStyle.cs).</summary>
public partial class BrowserHostHub
{
	public Task<string?> KcvAccentColor()
	{
		ForkSettings.KcvStyleSettings style = ForkSettings.Current.KcvStyle;
		return Task.FromResult(style.Enabled ? KcvStyle.Hex(KcvStyle.ColorsOf(style.AccentValue).Accent) : null);
	}
}
