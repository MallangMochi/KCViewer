using System;
using System.IO;
using KCViewer.Avalonia.Services;

namespace KCViewer.Fork;

/// <summary>
/// Fork: game images from the local server's assets folder (<see cref="ForkSettings.AssetsSettings"/>) before the
/// viewer's cache (Connection.SaveDataPath), which the fork never fills: it does not run the proxy that saved the
/// game's files there. The assets folder has the same layout (kcs2\resources\ship\banner\0001_2910.png). Both image
/// lookups use it: KCResourceHelper (ship graphic viewer, album images) and the Avalonia GameResourceHelper (fleet
/// image generator, sortie record viewer, ship selector).
/// </summary>
public static class ForkAssets
{
	private static readonly Lazy<string?> Resolved = new(() => ForkSettings.Current.Assets.ResolvedFolder);

	/// <summary>Found once, as images are looked up often (list rows); null without an assets folder.</summary>
	public static string? Folder => Resolved.Value;

	/// <summary>Where the ship graphic viewer's open dialog starts.</summary>
	public static string ShipImageFolder => Path.Combine(
		Folder ?? Utility.Configuration.Config.Connection.SaveDataPath,
		@"kcs2\resources\ship");

	/// <summary>Called at start (FormMainViewModel.AddForkViews).</summary>
	public static void Install()
	{
		GameResourceHelper.ForkAssetsFolder = Folder;
		Utility.Logger.Add(1, $"Assets: {Folder ?? "-"}");
	}
}
