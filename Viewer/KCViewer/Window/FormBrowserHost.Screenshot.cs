using System;

namespace KCViewer.Window;

/// <summary>Fork: the browser's state for the viewer's screenshots (Fork/ForkScreenshot.cs).</summary>
public partial class FormBrowserHost
{
	/// <summary>The browser process is running and connected, so it can take a screenshot.</summary>
	public bool ForkBrowserReady
	{
		get
		{
			try
			{
				return Hubs.Count > 0 && BrowserWnd != IntPtr.Zero && BrowserProcess is { HasExited: false };
			}
			catch (InvalidOperationException)
			{
				// the process object no longer has a process
				return false;
			}
		}
	}

	/// <summary>The browser process's id while it runs: its own windows (developer tools, the extra browser) count as the viewer's.</summary>
	public uint? ForkBrowserProcessId => ForkBrowserReady ? (uint)BrowserProcess.Id : null;
}
