using System;

namespace KCViewer.Plugins;

/// <summary>What the viewer offers a plugin. One host per plugin.</summary>
public interface IPluginHost
{
	/// <summary>Which instance the plugin runs in.</summary>
	ViewerInstance Instance { get; }

	/// <summary>UI culture name, for example "ko-KR".</summary>
	string Culture { get; }

	/// <summary>
	/// Folder for the plugin's own files: <c>&lt;working folder&gt;\PluginData\&lt;Id&gt;\</c>, so each instance (profile)
	/// has its own. Created on first use.
	/// </summary>
	string DataFolder { get; }

	/// <summary>
	/// An API call this instance's browser received. Raised on a background thread, in the order the calls finished.
	/// The request has the api_token value blanked.
	/// </summary>
	event EventHandler<ApiExchange>? ApiReceived;

	/// <summary>
	/// Receives the bodies of game files this instance's browser gets (see <see cref="GameFileResponse"/>).
	/// <paramref name="wants"/> is called for every game file response, on a background thread, and must return quickly.
	/// Only for the responses it accepts is the body read, from what the browser already received (nothing is requested
	/// from the server again), and passed to <paramref name="received"/> on a background thread.
	/// A body that could not be read is dropped without a call.
	/// </summary>
	void ReceiveGameFiles(Func<GameFileResponse, bool> wants, Action<GameFileResponse, byte[]> received);

	/// <summary>Raised on the UI thread once the main window has closed (after the exit confirmation).</summary>
	event EventHandler? ShuttingDown;

	/// <summary>Adds a dockable panel. It is listed under View &gt; Plugins; its position is saved with the layout.</summary>
	IPluginPanel AddPanel(PluginPanelOptions options);

	/// <summary>Writes to the viewer's log, prefixed with the plugin name.</summary>
	void Log(string message, PluginLogLevel level = PluginLogLevel.Info);
}

/// <summary>The instance a plugin runs in.</summary>
/// <param name="ProfileName">The <c>--profile</c> name, or null for the default working folder.</param>
/// <param name="IsTest">The test instance (profile "test"), which plays on the local server.</param>
/// <param name="IsEmbedded">Shown inside the live instance's test area panel.</param>
/// <param name="AppFolder">Folder of the viewer's exe.</param>
/// <param name="WorkingFolder">Working folder of this instance (settings, records, WebView2 profile).</param>
/// <param name="ExecutablePath">Path of the viewer's exe, for starting another instance.</param>
public sealed record ViewerInstance(
	string? ProfileName,
	bool IsTest,
	bool IsEmbedded,
	string AppFolder,
	string WorkingFolder,
	string ExecutablePath);

/// <summary>One API call as the browser captured it.</summary>
/// <param name="Time">When the response finished.</param>
/// <param name="Url">Full request URL.</param>
/// <param name="Api">Path after /kcsapi/, for example "api_port/port".</param>
/// <param name="Request">Form-encoded request body, api_token value blanked.</param>
/// <param name="Response">Response body as received ("svdata=" + JSON).</param>
public sealed record ApiExchange(DateTime Time, string Url, string Api, string Request, string Response);

public enum PluginLogLevel
{
	Debug = 1,
	Info = 2,
	Warning = 3,
}
