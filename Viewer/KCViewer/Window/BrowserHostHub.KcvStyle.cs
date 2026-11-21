using System.Threading.Tasks;
using KCViewer.Fork;

namespace KCViewer.Window;

/// <summary>Fork: the KanColleViewer style's accent for the browser process (Fork/KcvStyle.cs).</summary>
public partial class BrowserHostHub
{
	public Task<string?> KcvAccentColor() =>
		Task.FromResult<string?>(KcvStyle.Hex(KcvStyle.ColorsOf(ForkSettings.Current.KcvStyle.AccentValue).Accent));
}
