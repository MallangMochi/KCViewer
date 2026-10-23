namespace KCViewer.Plugins;

/// <summary>
/// A viewer plugin. Put it in <c>&lt;exe folder&gt;\Plugins\&lt;Name&gt;\&lt;Name&gt;.dll</c>; every public class in that
/// assembly that implements this interface and has a parameterless constructor is created at startup, in each
/// instance (live and test). Use <see cref="IPluginHost.Instance"/> to decide what to do in which instance.
/// </summary>
/// <remarks>
/// Plugins run with the same rights as the viewer, so only install plugins you wrote or reviewed.
/// The host never gives plugins the login token or any way to control the live browser.
/// </remarks>
public interface IViewerPlugin
{
	/// <summary>Stable identifier, for example "KCViewer.Plugin.TestArea". Names the plugin's data folder and panels.</summary>
	string Id { get; }

	/// <summary>Display name.</summary>
	string Name { get; }

	/// <summary>Called once at startup on the UI thread, before the saved panel layout is restored.</summary>
	void Initialize(IPluginHost host);
}
