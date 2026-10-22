using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserLibCore;

/// <summary>
/// Fork: rules shared by the browser process and the host for game files, the responses the game client loads from
/// /kcs2/, /kcs/ and /gadget_html5/ (images, sounds, scripts, JSON). Their bodies are passed on only when the host asks.
/// </summary>
public static class DevToolsGameFiles
{
	/// <summary>Body parts stay far below the 4 MB gRPC message limit; main.js alone is about 11 MB.</summary>
	public const int PartSize = 1024 * 1024;

	private static readonly string[] Folders = ["/kcs2/", "/kcs/", "/gadget_html5/"];

	/// <summary>
	/// A GET for a file under the game folders. Pages are left out: the game frame (/kcs2/index.php) carries the
	/// api_token in its URL.
	/// </summary>
	public static bool IsGameFile(Uri uri, string method, string? resourceType) =>
		uri.Scheme is "http" or "https"
		&& method == "GET"
		&& resourceType is not ("Document" or "Preflight")
		&& Folders.Any(folder => uri.AbsolutePath.StartsWith(folder, StringComparison.Ordinal))
		&& !uri.AbsolutePath.EndsWith(".php", StringComparison.OrdinalIgnoreCase);

	/// <summary>Blanks an api_token value in the query, the same way <see cref="DevToolsCapturePayload.BlankToken"/> does for request bodies.</summary>
	public static string BlankTokenInUrl(string url)
	{
		int query = url.IndexOf('?');
		if (query < 0) return url;

		int fragment = url.IndexOf('#', query);
		string tail = fragment < 0 ? "" : url[fragment..];
		string parameters = fragment < 0 ? url[(query + 1)..] : url[(query + 1)..fragment];
		return url[..(query + 1)] + DevToolsCapturePayload.BlankToken(parameters) + tail;
	}

	/// <summary>Response headers without cookies, which the page has no reason to share.</summary>
	public static Dictionary<string, string> WithoutCookies(IEnumerable<KeyValuePair<string, string>> headers) =>
		headers
			.Where(header => !header.Key.Equals("set-cookie", StringComparison.OrdinalIgnoreCase))
			.GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(group => group.Key, group => string.Join("\n", group.Select(header => header.Value)), StringComparer.OrdinalIgnoreCase);
}
