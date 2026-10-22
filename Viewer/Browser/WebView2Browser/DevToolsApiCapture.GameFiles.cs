using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using BrowserLibCore;

namespace Browser.WebView2Browser;

/// <summary>
/// Fork: game files (<see cref="DevToolsGameFiles"/>). Each finished response is offered to the host, and only when the
/// host wants it is the body read with Network.getResponseBody, from what the page already received, and sent on in
/// parts. Nothing is requested from the server again.
/// </summary>
public sealed partial class DevToolsApiCapture
{
	private long lastGameFileId;
	private bool gameFileOfferFailed;

	/// <summary>
	/// True when a response was answered by the local skin (LocalSkin.TakeAnswered). Such a response is not the
	/// game's file and is not offered, so the asset saver never takes a skin file for an official one.
	/// </summary>
	public Func<string, bool>? IsAnsweredLocally { get; set; }

	/// <summary>Bodies being read and sent at once; keeps memory flat when many new files arrive together.</summary>
	private SemaphoreSlim GameFileBodies { get; } = new(2);

	private static bool IsGameFile(string url, string? method, string? resourceType) =>
		Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && DevToolsGameFiles.IsGameFile(uri, method ?? "", resourceType);

	private static Dictionary<string, string> HeadersOf(JsonNode? headers)
	{
		List<KeyValuePair<string, string>> pairs = new();
		if (headers is JsonObject values)
		{
			foreach ((string name, JsonNode? value) in values)
			{
				pairs.Add(new(name, value?.GetValue<string>() ?? ""));
			}
		}

		return DevToolsGameFiles.WithoutCookies(pairs);
	}

	/// <remarks>Runs on the UI thread, which the WebView2 calls need; decoding and sending run on the thread pool.</remarks>
	private async Task OfferGameFileAsync(string sessionId, string requestId, Pending pending)
	{
		long id = Interlocked.Increment(ref lastGameFileId);
		try
		{
			bool wanted = await Host.GameFileReceived(id, DevToolsGameFiles.BlankTokenInUrl(pending.Url), pending.Status, pending.MimeType, pending.Headers);
			if (!wanted) return;
		}
		catch (Exception ex)
		{
			if (!gameFileOfferFailed) Log(3, ForkBrowserStrings.CaptureOfferFailed(ex.Message));
			gameFileOfferFailed = true;
			return;
		}

		await GameFileBodies.WaitAsync();
		try
		{
			byte[] body;
			try
			{
				string result = await WebView.CallDevToolsProtocolMethodForSessionAsync(sessionId, "Network.getResponseBody", $$"""{"requestId":"{{requestId}}"}""");
				body = await Task.Run(() => DecodeBody(result, pending.Headers.GetValueOrDefault("content-type")));
			}
			catch (Exception ex)
			{
				Log(3, ForkBrowserStrings.CaptureReadFailed(new Uri(pending.Url).AbsolutePath, ex.Message));
				await Host.GameFileFailed(id);
				return;
			}

			await Task.Run(async () =>
			{
				for (int start = 0; ; start += DevToolsGameFiles.PartSize)
				{
					int length = Math.Min(DevToolsGameFiles.PartSize, body.Length - start);
					bool isLast = start + length >= body.Length;
					await Host.GameFileBodyPart(id, body.AsSpan(start, length).ToArray(), isLast);
					if (isLast) break;
				}
			});
		}
		catch (Exception ex)
		{
			Log(3, ForkBrowserStrings.CapturePassFileFailed(new Uri(pending.Url).AbsolutePath, ex.Message));
		}
		finally
		{
			GameFileBodies.Release();
		}
	}

	/// <summary>
	/// Binary bodies come as base64 and are returned as received. Text bodies come already decoded, so they are encoded
	/// again with the charset of the response (UTF-8 when it names none); a byte order mark the file had is not in the text.
	/// </summary>
	private static byte[] DecodeBody(string getResponseBodyResult, string? contentType)
	{
		JsonNode result = JsonNode.Parse(getResponseBodyResult)!;
		string body = result["body"]!.GetValue<string>();
		if (result["base64Encoded"]?.GetValue<bool>() == true) return Convert.FromBase64String(body);

		return CharsetOf(contentType).GetBytes(body);
	}

	private static Encoding CharsetOf(string? contentType)
	{
		foreach (string part in (contentType ?? "").Split(';'))
		{
			string[] pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
			if (pair.Length != 2 || !pair[0].Equals("charset", StringComparison.OrdinalIgnoreCase)) continue;

			try
			{
				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
				return Encoding.GetEncoding(pair[1].Trim('"'));
			}
			catch (ArgumentException)
			{
				break;
			}
		}

		return Encoding.UTF8;
	}
}
