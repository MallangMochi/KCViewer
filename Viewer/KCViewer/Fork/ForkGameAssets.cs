using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using KCViewer.Avalonia.Services;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the game's files the viewer draws (the KanColleViewer icons, the ships' banners and other ship images), read
/// from the game's own files instead of copies in the build (user's decision 2026-10-07: the game's images are not ours
/// to publish). A file is looked up in the local server's assets folder (<see cref="ForkAssets.Folder"/>), then in the
/// viewer's own folder <c>&lt;exe folder&gt;\GameAssets</c>, laid out like the game (kcs2\...). A missing one is fetched
/// from the official server the local assets came from (<see cref="ForkSettings.AssetsSettings.DownloadServer"/>), apart
/// from the live browser and without a token, two at a time, each file once per start (a failure waits for the next
/// start). The icons' sprites are fetched at start; a ship's image only when a window asks for it (user's decision
/// 2026-10-07 "배너가 필요한 화면을 열었을때 해당 배너를 가져오도록").
/// </summary>
public static class ForkGameAssets
{
	/// <summary>The sprites (json and png) of the KanColleViewer icons: equipment, use items, the port's resources, the burner.</summary>
	public static IReadOnlyList<string> IconSprites { get; } =
	[
		@"kcs2\img\common\common_icon_weapon",
		@"kcs2\img\common\common_itemicons",
		@"kcs2\img\port\port_skin_1",
		@"kcs2\img\arsenal\arsenal_main",
	];

	/// <summary>The viewer's own folder for the game's files (fetched ones).</summary>
	public static string OwnFolder { get; } = Path.Combine(AppContext.BaseDirectory, "GameAssets");

	/// <summary>Raised on the UI thread when an icon sprite has been fetched (its path without the extension).</summary>
	public static event Action<string>? SpriteAvailable;

	private static HttpClient Client { get; } = new() { Timeout = TimeSpan.FromSeconds(60) };

	private static SemaphoreSlim Slots { get; } = new(2);

	/// <summary>Every fetch of this start by file, so a file is asked for once.</summary>
	private static ConcurrentDictionary<string, Task<bool>> Fetches { get; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Called at start (FormMainViewModel.AddForkViews): the folders for the windows of the viewer's Avalonia part.</summary>
	public static void Install()
	{
		GameResourceHelper.ForkGameAssetsFolder = OwnFolder;
		GameResourceHelper.ForkFetch = file => FetchAsync(file, quiet: true);
	}

	/// <summary>The full path of a game file (a path in the game's layout), or null when no folder has it.</summary>
	public static string? Find(string relativePath)
	{
		string file = Normalize(relativePath);
		foreach (string? folder in new[] { ForkAssets.Folder, OwnFolder })
		{
			if (folder is null) continue;

			string path = Path.Combine(folder, file);
			if (File.Exists(path)) return path;
		}

		return null;
	}

	/// <summary>Whether a sprite's json and png are both there.</summary>
	public static bool HasSprite(string sprite) => Find(sprite + ".json") is not null && Find(sprite + ".png") is not null;

	/// <summary>Called at start (FormMainViewModel.AddForkViews): fetches the icon sprites no folder has, in the background.</summary>
	public static void PreloadIcons()
	{
		List<string> missing = IconSprites.Where(sprite => !HasSprite(sprite)).ToList();
		if (missing.Count == 0) return;

		if (Server is not { } server)
		{
			Utility.Logger.Add(2, ForkStrings.GameAssetsNoServer(missing.Count));
			return;
		}

		Utility.Logger.Add(2, ForkStrings.GameAssetsFetching(missing.Count, server));

		foreach (string sprite in missing)
		{
			_ = Task.Run(async () =>
			{
				if (!await FetchAsync(sprite + ".json") || !await FetchAsync(sprite + ".png")) return;

				Application.Current?.Dispatcher.InvokeAsync(() => SpriteAvailable?.Invoke(sprite));
			});
		}
	}

	/// <summary>
	/// A game file (a path in the game's layout, either slash) in a folder, fetched into <see cref="OwnFolder"/> when none
	/// has it: true when it is there afterwards. <paramref name="quiet"/>: a window's image, logged only when it fails.
	/// </summary>
	public static Task<bool> FetchAsync(string relativePath, bool quiet = false)
	{
		string file = Normalize(relativePath);
		if (Find(file) is not null) return Task.FromResult(true);
		if (Server is not { } server) return Task.FromResult(false);

		return Fetches.GetOrAdd(file, _ => Task.Run(() => Fetch(server, file, quiet)));
	}

	private static string? Server => ForkSettings.Current.Assets.DownloadServer.Trim().TrimEnd('/') is { Length: > 0 } server ? server : null;

	private static string Normalize(string relativePath) => relativePath.Replace('/', '\\').TrimStart('\\');

	private static async Task<bool> Fetch(string server, string file, bool quiet)
	{
		string url = $"{server}/{file.Replace('\\', '/')}";
		string path = Path.Combine(OwnFolder, file);

		await Slots.WaitAsync();
		try
		{
			byte[] data = await Client.GetByteArrayAsync(url);
			if (!LooksRight(file, data)) throw new InvalidDataException(ForkStrings.GameAssetNotGameFile);

			Directory.CreateDirectory(Path.GetDirectoryName(path)!);

			// written next to it and then swapped in, so a failed write never leaves half a file
			string temporary = path + ".download";
			await File.WriteAllBytesAsync(temporary, data);
			File.Move(temporary, path, overwrite: true);

			Utility.Logger.Add(quiet ? 1 : 2, ForkStrings.GameAssetFetched(file, data.Length));
			return true;
		}
		catch (Exception e)
		{
			// a window's image the server does not have (one the game never made) is not worth a visible line
			bool missing = e is HttpRequestException { StatusCode: System.Net.HttpStatusCode.NotFound };
			Utility.Logger.Add(quiet ? (missing ? 1 : 2) : 3, ForkStrings.GameAssetFetchFailed(file, e.Message));
			return false;
		}
		finally
		{
			Slots.Release();
		}
	}

	/// <summary>A png starts with its signature and a json parses, so an error page is never kept.</summary>
	private static bool LooksRight(string file, byte[] data)
	{
		if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
		{
			return data.Length > 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
		}

		try
		{
			using JsonDocument json = JsonDocument.Parse(data);
			return json.RootElement.TryGetProperty("frames", out _);
		}
		catch (JsonException)
		{
			return false;
		}
	}
}
