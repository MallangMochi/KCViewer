using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;
using KCViewer.Observer;
using KCViewer.Utility;
using WpfWindow = System.Windows.Window;

namespace KCViewer.Fork;

/// <summary>
/// Fork, for checking the panels without playing: <c>--replay-api=FILE</c> (absolute path) feeds API traffic from a
/// JSONL file into the viewer once the main window is up, as if the browser had captured it. Each line is
/// <c>{ "path": "/kcsapi/...", "request": "api_verno=1&amp;...", "response": "svdata={...}" }</c>; request is optional.
/// A line <c>{ "wait": 5000 }</c> pauses that many milliseconds, so the panels draw between two responses (as they do
/// in play, where a later response changes what is already on screen).
/// Each response is also saved in the record database as captured traffic is (sortie, expedition and senka records),
/// with the time of the replay; tools/viewer-style-test/record-times.js puts recorded times on them afterwards.
/// Only with a test profile (<c>--profile=NAME</c>, not "live"), so replayed data never mixes with the live account's
/// records. Nothing is sent anywhere; the browser is not involved.
/// </summary>
public static class ApiReplay
{
	public const string Prefix = "--replay-api=";

	public static void Install(WpfWindow window)
	{
		string? file = ForkOptions.Argument(Prefix);
		if (file is null) return;

		if (ForkProfile.Name is null or "live")
		{
			Logger.Add(3, $"{Prefix} is ignored without a test profile (--profile=NAME).");
			return;
		}

		window.Loaded += (_, _) => window.Dispatcher.BeginInvoke(async () => await Replay(file), DispatcherPriority.ApplicationIdle);
	}

	private static async Task Replay(string file)
	{
		int count = 0;
		try
		{
			foreach (string line in File.ReadLines(file))
			{
				if (string.IsNullOrWhiteSpace(line)) continue;

				using JsonDocument json = JsonDocument.Parse(line);
				JsonElement root = json.RootElement;

				if (root.TryGetProperty("wait", out JsonElement wait))
				{
					// back on the UI thread afterwards: the panels have drawn the responses so far
					await Task.Delay(wait.GetInt32());
					continue;
				}

				string path = root.GetProperty("path").GetString()!;
				string? body = root.TryGetProperty("request", out JsonElement request) ? request.GetString() : null;
				string response = root.GetProperty("response").GetString()!;

				await APIObserver.Instance.SaveReplayedApiFile(path, body ?? "", response);

				if (body is { Length: > 0 })
				{
					APIObserver.Instance.LoadRequest(path, body);
				}

				APIObserver.Instance.LoadResponse(path, response);
				count++;
			}

			Logger.Add(2, $"Replayed {count} API responses from {file}");
		}
		catch (Exception ex)
		{
			Logger.Add(3, $"{Prefix}{file}: {ex.Message} (after {count} responses)");
		}
	}
}
