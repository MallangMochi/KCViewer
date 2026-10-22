using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Threading.Tasks;
using BrowserLibCore;
using Microsoft.Web.WebView2.Core;

namespace Browser.WebView2Browser;

/// <summary>
/// Fork: reads the game traffic that the page has already received, through the DevTools protocol (CDP),
/// and hands it to the host. Replaces the Titanium MITM proxy, so there is no root certificate and no listening port.
/// </summary>
/// <remarks>
/// Read-only by design: only Network events, <c>Network.getResponseBody</c> and <c>Network.getRequestPostData</c> are used.
/// Nothing is sent to the game server, no request is modified, no input is dispatched.
/// On the DMM page the game runs in a cross-site iframe, which Chromium puts in its own process,
/// so child targets are attached the same way the DevTools window does it (Target.setAutoAttach).
/// The api_token value is blanked here, before anything leaves the browser process.
/// Game files (images, sounds, scripts) are offered to the host too; see DevToolsApiCapture.GameFiles.cs.
/// </remarks>
public sealed partial class DevToolsApiCapture
{
	private enum Kind
	{
		Api,
		/// <summary>Only a game file (DevToolsApiCapture.GameFiles.cs).</summary>
		GameFile,
	}

	private sealed class Pending
	{
		public required Kind Kind { get; init; }
		public required string Url { get; init; }
		public string? PostData { get; init; }
		public bool HasPostData { get; init; }
		public int Status { get; set; }
		public string MimeType { get; set; } = "";
		public bool IsGameFile { get; init; }
		public Dictionary<string, string> Headers { get; set; } = new();
	}

	private sealed record Delivery(string Url, string RequestBody, string ResponseBody);

	private CoreWebView2 WebView { get; }
	private IBrowserHost Host { get; }
	private Action<int, string> Log { get; }
	private Dictionary<string, Pending> PendingRequests { get; } = new();

	/// <summary>
	/// Body fetches run in parallel, but deliveries are awaited in the order the responses finished,
	/// so the host sees the API calls in game order.
	/// </summary>
	private Channel<Task<Delivery?>> Deliveries { get; } = Channel.CreateUnbounded<Task<Delivery?>>(new() { SingleReader = true });

	private HashSet<string> SessionsWithApi { get; } = new();

	public DevToolsApiCapture(CoreWebView2 webView, IBrowserHost host, Action<int, string> log)
	{
		WebView = webView;
		Host = host;
		Log = log;
	}

	public async Task StartAsync()
	{
		Subscribe("Network.requestWillBeSent", OnRequestWillBeSent);
		Subscribe("Network.responseReceived", OnResponseReceived);
		Subscribe("Network.loadingFinished", OnLoadingFinished);
		Subscribe("Network.loadingFailed", OnLoadingFailed);
		Subscribe("Target.attachedToTarget", OnAttachedToTarget);
		_ = Task.Run(DeliverAsync);
		await EnableSessionAsync("");
	}

	private void Subscribe(string eventName, Func<string, JsonNode, Task> handler)
	{
		WebView.GetDevToolsProtocolEventReceiver(eventName).DevToolsProtocolEventReceived += async (_, e) =>
		{
			try
			{
				await handler(e.SessionId ?? "", JsonNode.Parse(e.ParameterObjectAsJson)!);
			}
			catch (Exception ex)
			{
				Log(3, ForkBrowserStrings.CaptureEventFailed(eventName, ex.Message));
			}
		};
	}

	private async Task<JsonNode> CallAsync(string sessionId, string method, object? parameters = null)
	{
		string json = parameters is null ? "{}" : JsonSerializer.Serialize(parameters);
		string result = sessionId.Length == 0
			? await WebView.CallDevToolsProtocolMethodAsync(method, json)
			: await WebView.CallDevToolsProtocolMethodForSessionAsync(sessionId, method, json);
		return JsonNode.Parse(result)!;
	}

	private async Task EnableSessionAsync(string sessionId)
	{
		// Large buffers so getResponseBody still has api_start2 (several MB) when loadingFinished arrives.
		await CallAsync(sessionId, "Network.enable", new { maxTotalBufferSize = 100_000_000, maxResourceBufferSize = 50_000_000, maxPostDataSize = 1_000_000 });
		// waitForDebuggerOnStart pauses new child frames until Network.enable is on, so their first requests are not missed.
		await CallAsync(sessionId, "Target.setAutoAttach", new { autoAttach = true, waitForDebuggerOnStart = true, flatten = true });
	}

	private async Task OnAttachedToTarget(string parentSessionId, JsonNode p)
	{
		string child = p["sessionId"]!.GetValue<string>();
		try
		{
			await EnableSessionAsync(child);
		}
		catch (Exception ex)
		{
			Log(3, ForkBrowserStrings.CaptureChildFrameFailed(p["targetInfo"]?["type"]?.ToString(), ex.Message));
		}
		finally
		{
			// The child stays paused until this runs, so it must run even when enabling failed.
			await CallAsync(child, "Runtime.runIfWaitingForDebugger");
		}
	}

	private Task OnRequestWillBeSent(string sessionId, JsonNode p)
	{
		JsonNode request = p["request"]!;
		string url = request["url"]!.GetValue<string>();
		Kind? kind = KindOf(url);
		bool isGameFile = IsGameFile(url, request["method"]?.GetValue<string>(), p["type"]?.GetValue<string>());
		if (kind is not null || isGameFile)
		{
			PendingRequests[Key(sessionId, p)] = new Pending
			{
				Kind = kind ?? Kind.GameFile,
				Url = url,
				PostData = request["postData"]?.GetValue<string>(),
				HasPostData = request["hasPostData"]?.GetValue<bool>() ?? false,
				IsGameFile = isGameFile,
			};
		}
		return Task.CompletedTask;
	}

	private Task OnResponseReceived(string sessionId, JsonNode p)
	{
		if (PendingRequests.TryGetValue(Key(sessionId, p), out Pending? pending))
		{
			pending.Status = p["response"]?["status"]?.GetValue<int>() ?? 0;
			pending.MimeType = p["response"]?["mimeType"]?.GetValue<string>() ?? "";
			if (pending.IsGameFile) pending.Headers = HeadersOf(p["response"]?["headers"]);
		}
		return Task.CompletedTask;
	}

	private async Task OnLoadingFinished(string sessionId, JsonNode p)
	{
		if (!PendingRequests.Remove(Key(sessionId, p), out Pending? pending)) return;

		if (pending.IsGameFile && IsAnsweredLocally?.Invoke(pending.Url) != true)
		{
			_ = OfferGameFileAsync(sessionId, p["requestId"]!.GetValue<string>(), pending);
		}
		if (pending.Kind == Kind.GameFile) return;

		// Same filter as the proxy had (APIObserver.ProxyOnBeforeResponse): API answers are 200 text/plain.
		if (pending.Status != 200 || pending.MimeType != "text/plain") return;

		if (SessionsWithApi.Add(sessionId))
		{
			Log(2, ForkBrowserStrings.CaptureApiFrom(new Uri(pending.Url).GetLeftPart(UriPartial.Authority), sessionId.Length == 0));
		}

		string requestId = p["requestId"]!.GetValue<string>();
		await Deliveries.Writer.WriteAsync(FetchAsync(sessionId, requestId, pending));
	}

	private Task OnLoadingFailed(string sessionId, JsonNode p)
	{
		if (PendingRequests.Remove(Key(sessionId, p), out Pending? pending) && pending.Kind == Kind.Api)
		{
			Log(3, ForkBrowserStrings.CaptureRequestFailed(new Uri(pending.Url).AbsolutePath, p["errorText"]?.ToString()));
		}
		return Task.CompletedTask;
	}

	private async Task<Delivery?> FetchAsync(string sessionId, string requestId, Pending pending)
	{
		try
		{
			JsonNode body = await CallAsync(sessionId, "Network.getResponseBody", new { requestId });
			string response = body["body"]!.GetValue<string>();
			if (body["base64Encoded"]?.GetValue<bool>() == true)
			{
				response = Encoding.UTF8.GetString(Convert.FromBase64String(response));
			}

			string request = pending.PostData ?? "";
			if (request.Length == 0 && pending.HasPostData)
			{
				JsonNode post = await CallAsync(sessionId, "Network.getRequestPostData", new { requestId });
				request = post["postData"]?.GetValue<string>() ?? "";
			}

			return new Delivery(pending.Url, DevToolsCapturePayload.BlankToken(request), response);
		}
		catch (Exception ex)
		{
			Log(3, ForkBrowserStrings.CaptureReadFailed(new Uri(pending.Url).AbsolutePath, ex.Message));
			return null;
		}
	}

	private async Task DeliverAsync()
	{
		await foreach (Task<Delivery?> next in Deliveries.Reader.ReadAllAsync())
		{
			try
			{
				Delivery? delivery = await next;
				if (delivery is null) continue;

				await Host.ApiCaptured(delivery.Url, delivery.RequestBody, DevToolsCapturePayload.Compress(delivery.ResponseBody));
			}
			catch (Exception ex)
			{
				Log(3, ForkBrowserStrings.CapturePassFailed(ex.Message));
			}
		}
	}

	private static Kind? KindOf(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return null;
		// The server list used to come from /gadget_html5/js/kcs_const.js; upstream now has it built in (ServerManager, #700).
		if (uri.AbsolutePath.StartsWith("/kcsapi/", StringComparison.Ordinal)) return Kind.Api;
		return null;
	}

	private static string Key(string sessionId, JsonNode p) => sessionId + "|" + p["requestId"]!.GetValue<string>();
}
