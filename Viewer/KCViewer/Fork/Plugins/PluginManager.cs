using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using KCViewer.Plugins;
using KCViewer.Utility;

namespace KCViewer.Fork.Plugins;

/// <summary>A plugin found at startup.</summary>
public sealed record LoadedPlugin(string Folder, string? Id, string? Name, string? Version, string? Error);

/// <summary>
/// Fork: finds and starts the plugins in <c>&lt;exe folder&gt;\Plugins\&lt;Name&gt;\&lt;Name&gt;.dll</c>.
/// Plugins load into the default context, so their WPF views work like the viewer's own; a plugin's other files
/// are found in its folder. A plugin that fails to load or initialize is logged and skipped.
/// </summary>
public static class PluginManager
{
	private static readonly List<PluginHost> Hosts = new();
	private static readonly List<string> Folders = new();

	public static List<LoadedPlugin> Plugins { get; } = new();

	public static string PluginsFolder => Path.Combine(AppContext.BaseDirectory, "Plugins");

	public static void LoadAll(Action<PluginPanelViewModel> addPanel)
	{
		if (!Directory.Exists(PluginsFolder)) return;

		if (ForkOptions.NoPlugins)
		{
			Logger.Add(2, ForkStrings.PluginsNotLoaded);
			return;
		}

		AssemblyLoadContext.Default.Resolving += ResolveFromPluginFolders;

		foreach (string folder in Directory.GetDirectories(PluginsFolder).Order())
		{
			string name = Path.GetFileName(folder);
			string assemblyPath = Path.Combine(folder, name + ".dll");
			if (!File.Exists(assemblyPath)) continue;

			Folders.Add(folder);
			Load(folder, assemblyPath, addPanel);
		}
	}

	private static void Load(string folder, string assemblyPath, Action<PluginPanelViewModel> addPanel)
	{
		Assembly assembly;
		try
		{
			assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
		}
		catch (Exception ex)
		{
			Fail(folder, null, ex);
			return;
		}

		string? version = assembly.GetName().Version?.ToString();
		IEnumerable<Type> types;
		try
		{
			types = assembly.GetExportedTypes()
				.Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IViewerPlugin).IsAssignableFrom(t));
		}
		catch (Exception ex)
		{
			Fail(folder, null, ex);
			return;
		}

		foreach (Type type in types)
		{
			IViewerPlugin? plugin = null;
			try
			{
				plugin = (IViewerPlugin)Activator.CreateInstance(type)!;
				PluginHost host = new(plugin, addPanel);
				plugin.Initialize(host);
				Hosts.Add(host);
				Plugins.Add(new LoadedPlugin(folder, plugin.Id, plugin.Name, version, null));
				Logger.Add(2, ForkStrings.PluginLoaded(plugin.Name, version));
			}
			catch (Exception ex)
			{
				Fail(folder, plugin, ex);
			}
		}
	}

	private static void Fail(string folder, IViewerPlugin? plugin, Exception ex)
	{
		Plugins.Add(new LoadedPlugin(folder, plugin?.Id, plugin?.Name, null, ex.Message));
		Logger.Add(3, ForkStrings.PluginFailed(Path.GetFileName(folder), ex.Message));
	}

	/// <summary>Called once the main window has closed.</summary>
	public static void ShutDown()
	{
		foreach (PluginHost host in Hosts)
		{
			host.RaiseShuttingDown();
		}
	}

	private static Assembly? ResolveFromPluginFolders(AssemblyLoadContext context, AssemblyName name)
	{
		foreach (string folder in Folders)
		{
			string path = Path.Combine(folder, name.Name + ".dll");
			if (File.Exists(path)) return context.LoadFromAssemblyPath(path);
		}

		return null;
	}
}
