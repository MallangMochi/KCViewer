using System;
using System.Globalization;
using System.IO;
using KCViewer.Plugins;
using KCViewer.Utility;

namespace KCViewer.Fork.Plugins;

/// <summary>
/// Fork: the host one plugin sees. It passes on API calls and the shutdown notice, and keeps a failing
/// plugin handler from affecting the viewer or other plugins.
/// </summary>
internal sealed class PluginHost : IPluginHost
{
	private readonly IViewerPlugin plugin;
	private readonly Action<PluginPanelViewModel> addPanel;

	public PluginHost(IViewerPlugin plugin, Action<PluginPanelViewModel> addPanel)
	{
		this.plugin = plugin;
		this.addPanel = addPanel;
		ApiEvents.Received += OnApiReceived;
	}

	public ViewerInstance Instance { get; } = new(
		ForkProfile.Name,
		ForkProfile.IsTest,
		ForkProfile.IsEmbedded,
		AppContext.BaseDirectory,
		Directory.GetCurrentDirectory(),
		Environment.ProcessPath!);

	public string Culture => CultureInfo.CurrentUICulture.Name;

	public string DataFolder
	{
		get
		{
			string folder = Path.Combine(Directory.GetCurrentDirectory(), "PluginData", plugin.Id);
			Directory.CreateDirectory(folder);
			return folder;
		}
	}

	public event EventHandler<ApiExchange>? ApiReceived;
	public event EventHandler? ShuttingDown;

	public void ReceiveGameFiles(Func<GameFileResponse, bool> wants, Action<GameFileResponse, byte[]> received)
	{
		GameFileEvents.Add(this, wants, received);
	}

	public IPluginPanel AddPanel(PluginPanelOptions options)
	{
		PluginPanelViewModel panel = new(plugin.Id, options);
		addPanel(panel);
		return panel;
	}

	public void Log(string message, PluginLogLevel level = PluginLogLevel.Info)
	{
		Logger.Add((int)level, $"[{plugin.Name}] {message}");
	}

	private void OnApiReceived(ApiExchange exchange)
	{
		try
		{
			ApiReceived?.Invoke(this, exchange);
		}
		catch (Exception ex)
		{
			Log($"API handler failed: {ex.Message}", PluginLogLevel.Warning);
		}
	}

	internal void RaiseShuttingDown()
	{
		try
		{
			ShuttingDown?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Log($"shutdown handler failed: {ex.Message}", PluginLogLevel.Warning);
		}
	}
}
