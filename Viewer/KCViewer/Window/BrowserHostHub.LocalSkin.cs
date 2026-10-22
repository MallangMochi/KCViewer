using System.Threading.Tasks;
using KCViewer.Fork;

namespace KCViewer.Window;

/// <summary>Fork: tells the browser process where the local skin is (Browser/WebView2Browser/LocalSkin.cs).</summary>
public partial class BrowserHostHub
{
	public Task<string?> LocalSkinFolder()
	{
		ForkSettings.LocalSkinSettings skin = ForkSettings.Current.LocalSkin;
		return Task.FromResult(skin.Enabled ? skin.ResolvedFolder : null);
	}
}
