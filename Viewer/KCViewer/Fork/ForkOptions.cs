using System;
using System.Linq;
using KCViewer.Utility;

namespace KCViewer.Fork;

/// <summary>
/// Fork-only switches, kept in one place so the edits to upstream files stay small.
/// </summary>
public static class ForkOptions
{
	/// <summary>
	/// Game traffic is read through DevTools in the browser process (Browser/WebView2Browser/DevToolsApiCapture.cs).
	/// The Titanium MITM proxy is never started, so no root certificate is installed and no port is opened.
	/// </summary>
	public static bool UseDevToolsCapture => true;

	/// <summary>
	/// Upstream program updates are not installed: they would replace the fork's files with the upstream release.
	/// Data and translation updates still run. Upstream code changes are merged with git.
	/// </summary>
	public static bool DisableSoftwareUpdate => true;

	/// <summary>
	/// UI language of a new configuration: Korean whatever the Windows language (user decision), with the ship and
	/// equipment names translated. Upstream started in Japanese only on a Japanese system and in English otherwise.
	/// </summary>
	public static string DefaultCulture => "ko-KR";

	/// <summary>
	/// <c>--start-url=...</c> overrides the browser's start page for this run only; it is not saved.
	/// Used to open the local server in the test instance and in local tests.
	/// </summary>
	public static string? StartUrl { get; } = Argument(KCViewer.Plugins.ViewerCommandLine.StartUrlPrefix);

	/// <summary>
	/// <c>--no-plugins</c>: start without plugins, for example to check the panels with a test profile while the
	/// local server, the test area and the recorders stay off.
	/// </summary>
	public static bool NoPlugins { get; } = Environment.GetCommandLineArgs().Contains("--no-plugins");

	/// <summary>Value of the last <c>prefix=value</c> command line argument, or null.</summary>
	public static string? Argument(string prefix) => Environment.GetCommandLineArgs()
		.Where(arg => arg.StartsWith(prefix, StringComparison.Ordinal))
		.Select(arg => arg[prefix.Length..])
		.LastOrDefault();

	/// <summary>
	/// Proxy for the browser while the MITM proxy is off: an explicit downstream proxy if one is set,
	/// otherwise the configured upstream proxy (for example KCCacheProxy), otherwise a direct connection.
	/// </summary>
	public static string BrowserProxy(Configuration.ConfigurationData.ConfigConnection config) =>
		!string.IsNullOrEmpty(config.DownstreamProxy) ? config.DownstreamProxy
		: config.UseUpstreamProxy ? $"{config.UpstreamProxyAddress}:{config.UpstreamProxyPort}"
		: "direct://";
}
