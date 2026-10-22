using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BrowserLibCore;
using KCViewer.Plugins;

namespace KCViewer.Fork.Plugins;

/// <summary>
/// Fork: game files from the browser (<see cref="DevToolsGameFiles"/>) for the plugins that asked for them with
/// <see cref="IPluginHost.ReceiveGameFiles"/>. The browser offers each response; a body comes only when some plugin
/// wants it, in parts, and is handed to those plugins on the thread pool.
/// </summary>
internal static class GameFileEvents
{
	private sealed record Receiver(PluginHost Host, Func<GameFileResponse, bool> Wants, Action<GameFileResponse, byte[]> Received);

	private sealed record Incoming(GameFileResponse Response, List<Receiver> Receivers, MemoryStream Body);

	/// <summary>Bodies the browser has not finished within this time are given up (the browser may have been restarted).</summary>
	private static readonly TimeSpan IncomingLimit = TimeSpan.FromMinutes(5);

	private static readonly object Gate = new();
	private static readonly List<Receiver> Receivers = new();
	private static readonly Dictionary<long, Incoming> IncomingBodies = new();

	public static void Add(PluginHost host, Func<GameFileResponse, bool> wants, Action<GameFileResponse, byte[]> received)
	{
		lock (Gate) Receivers.Add(new Receiver(host, wants, received));
	}

	/// <summary>Called for every game file response. True: send the body.</summary>
	public static bool Offer(long id, string url, int status, string mimeType, Dictionary<string, string> headers)
	{
		List<Receiver> receivers;
		lock (Gate)
		{
			if (Receivers.Count == 0) return false;
			receivers = Receivers.ToList();
		}

		if (!Uri.TryCreate(DevToolsGameFiles.BlankTokenInUrl(url), UriKind.Absolute, out Uri? uri)) return false;
		// The browser already applies these rules; apply them again so no other path can pass a page, a token or a cookie on.
		if (!DevToolsGameFiles.IsGameFile(uri, "GET", null)) return false;

		GameFileResponse response = new(
			DateTime.Now,
			uri.OriginalString,
			uri.Host,
			uri.AbsolutePath,
			uri.Query,
			status,
			mimeType,
			DevToolsGameFiles.WithoutCookies(headers));

		List<Receiver> wanting = receivers.Where(receiver => Wants(receiver, response)).ToList();
		if (wanting.Count == 0) return false;

		lock (Gate)
		{
			DateTime oldest = DateTime.Now - IncomingLimit;
			foreach (long stale in IncomingBodies.Where(pair => pair.Value.Response.Time < oldest).Select(pair => pair.Key).ToList())
			{
				IncomingBodies.Remove(stale);
			}

			IncomingBodies[id] = new Incoming(response, wanting, new MemoryStream());
		}

		return true;
	}

	public static void AddPart(long id, byte[] part, bool isLast)
	{
		Incoming? incoming;
		lock (Gate)
		{
			if (!IncomingBodies.TryGetValue(id, out incoming)) return;

			incoming.Body.Write(part);
			if (!isLast) return;

			IncomingBodies.Remove(id);
		}

		byte[] body = incoming.Body.ToArray();
		foreach (Receiver receiver in incoming.Receivers)
		{
			_ = Task.Run(() => Deliver(receiver, incoming.Response, body));
		}
	}

	public static void Fail(long id)
	{
		lock (Gate) IncomingBodies.Remove(id);
	}

	private static bool Wants(Receiver receiver, GameFileResponse response)
	{
		try
		{
			return receiver.Wants(response);
		}
		catch (Exception ex)
		{
			receiver.Host.Log($"game file filter failed: {ex.Message}", PluginLogLevel.Warning);
			return false;
		}
	}

	private static void Deliver(Receiver receiver, GameFileResponse response, byte[] body)
	{
		try
		{
			receiver.Received(response, body);
		}
		catch (Exception ex)
		{
			receiver.Host.Log($"game file handler failed ({response.Path}): {ex.Message}", PluginLogLevel.Warning);
		}
	}
}
