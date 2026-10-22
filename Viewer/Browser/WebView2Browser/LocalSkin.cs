using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Browser.WebView2Browser;

/// <summary>
/// Fork: the local skin (requirement 12). While it is on, the game's image and sound requests under /kcs2/ and /kcs/
/// are answered with the file of the same path in the skin folder, when there is one. The request does not reach the
/// server; only what the page shows or plays changes.
/// </summary>
/// <remarks>
/// Requirement 8, item 6: only images and sounds are ever answered, by extension; scripts, JSON and /kcsapi/ never.
/// Off by default. The answers are not cached (no-store), so turning the skin off and reloading the game brings the
/// original files back. Answered responses are left out of the game file capture, so the asset saver never takes a
/// skin file for an official one.
/// </remarks>
public sealed class LocalSkin
{
	private static readonly string[] Folders = ["/kcs2/", "/kcs/"];

	private static readonly string[] UriFilters = ["*/kcs2/*", "*/kcs/*"];

	private static readonly CoreWebView2WebResourceContext[] Contexts =
	[
		CoreWebView2WebResourceContext.Image,
		CoreWebView2WebResourceContext.Media,
		CoreWebView2WebResourceContext.XmlHttpRequest,
		CoreWebView2WebResourceContext.Fetch,
	];

	private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		[".png"] = "image/png",
		[".jpg"] = "image/jpeg",
		[".jpeg"] = "image/jpeg",
		[".gif"] = "image/gif",
		[".webp"] = "image/webp",
		[".mp3"] = "audio/mpeg",
		[".ogg"] = "audio/ogg",
		[".m4a"] = "audio/mp4",
		[".wav"] = "audio/wav",
	};

	private CoreWebView2 WebView { get; }
	private CoreWebView2Environment Environment { get; }
	private Action<int, string> Log { get; }

	/// <summary>The skin folder and the game paths it has files for; null while the skin is off.</summary>
	private (string Folder, HashSet<string> Paths)? Skin { get; set; }

	private bool FiltersAdded { get; set; }

	/// <summary>URLs answered from the skin folder whose response the game file capture has not seen yet.</summary>
	private ConcurrentDictionary<string, int> Answered { get; } = new();

	private int answeredCount;

	public LocalSkin(CoreWebView2 webView, CoreWebView2Environment environment, Action<int, string> log)
	{
		WebView = webView;
		Environment = environment;
		Log = log;
		WebView.WebResourceRequested += OnWebResourceRequested;
	}

	/// <summary>Turns the skin on with <paramref name="folder"/> (read again each time) or off with null.</summary>
	public async Task ApplyAsync(string? folder)
	{
		HashSet<string>? paths = folder is null ? null : await Task.Run(() => Scan(folder));
		Skin = folder is null ? null : (folder, paths!);
		Answered.Clear();

		// Filters only while on, so the skin costs nothing while it is off. Iframes (the DMM game frame) are included.
		if (Skin is not null && !FiltersAdded)
		{
			foreach (string uri in UriFilters)
			foreach (CoreWebView2WebResourceContext context in Contexts)
			{
				WebView.AddWebResourceRequestedFilter(uri, context, CoreWebView2WebResourceRequestSourceKinds.Document);
			}
			FiltersAdded = true;
		}
		else if (Skin is null && FiltersAdded)
		{
			foreach (string uri in UriFilters)
			foreach (CoreWebView2WebResourceContext context in Contexts)
			{
				WebView.RemoveWebResourceRequestedFilter(uri, context, CoreWebView2WebResourceRequestSourceKinds.Document);
			}
			FiltersAdded = false;
		}

		Log(2, Skin is { } skin ? ForkBrowserStrings.LocalSkinOn(skin.Paths.Count, skin.Folder) : ForkBrowserStrings.LocalSkinOff);
	}

	/// <summary>
	/// For the game file capture: true, once per answer, when the response for <paramref name="url"/> came from the
	/// skin folder.
	/// </summary>
	public bool TakeAnswered(string url)
	{
		string key = Key(url);
		while (Answered.TryGetValue(key, out int count))
		{
			if (count <= 1 ? Answered.TryRemove(new(key, count)) : Answered.TryUpdate(key, count - 1, count)) return true;
		}
		return false;
	}

	/// <summary>Game paths (for example "/kcs2/img/title/01.png") of the files that may be answered.</summary>
	private static HashSet<string> Scan(string folder)
	{
		HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
		if (!Directory.Exists(folder)) return paths;

		foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
		{
			string path = "/" + Path.GetRelativePath(folder, file).Replace('\\', '/');
			if (IsReplaceable(path)) paths.Add(path);
		}

		return paths;
	}

	private static bool IsReplaceable(string path) =>
		Folders.Any(folder => path.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
		&& ContentTypes.ContainsKey(Path.GetExtension(path))
		&& !path.Split('/').Any(segment => segment is "." or "..");

	private static string Key(string url) =>
		Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ? uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped) : url;

	private async void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
	{
		if (Skin is not { } skin || skin.Paths.Count == 0) return;
		if (e.Request.Method != "GET") return;
		if (!Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https")) return;

		string path = Uri.UnescapeDataString(uri.AbsolutePath);
		if (!IsReplaceable(path) || !skin.Paths.Contains(path)) return;

		// Only paths found in the skin folder get here, so the file name cannot leave it.
		string file = Path.Combine(skin.Folder, path[1..].Replace('/', Path.DirectorySeparatorChar));
		CoreWebView2Deferral deferral = e.GetDeferral();
		try
		{
			byte[] body = await File.ReadAllBytesAsync(file);
			string headers = string.Join("\r\n",
				$"Content-Type: {ContentTypes[Path.GetExtension(path)]}",
				$"Content-Length: {body.Length}",
				"Cache-Control: no-store",
				"Access-Control-Allow-Origin: *");
			Answered.AddOrUpdate(Key(e.Request.Uri), 1, (_, count) => count + 1);
			e.Response = Environment.CreateWebResourceResponse(new MemoryStream(body), 200, "OK", headers);

			int total = Interlocked.Increment(ref answeredCount);
			if (total == 1 || total % 100 == 0) Log(2, ForkBrowserStrings.LocalSkinAnswered(total, path));
		}
		catch (Exception ex)
		{
			// The request goes to the server as usual.
			Log(3, ForkBrowserStrings.LocalSkinReadFailed(file, ex.Message));
		}
		finally
		{
			deferral.Complete();
		}
	}
}
