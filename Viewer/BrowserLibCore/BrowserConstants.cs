#if !DEBUG
using System.IO;
#endif

namespace BrowserLibCore;

public class BrowserConstants
{
#if DEBUG
	public static string WebView2CachePath => "BrowserCache";
#else
	// Fork: kept in the working folder instead of %LOCALAPPDATA%, so each instance (live/test) has its own
	// WebView2 profile and the whole folder stays portable. The browser process inherits the host's working folder.
	public static string WebView2CachePath => Path.Combine(Directory.GetCurrentDirectory(), "WebView2");
#endif
}
