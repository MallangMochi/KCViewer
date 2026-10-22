using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using KCViewer.Notifier;

namespace KCViewer.Fork;

/// <summary>
/// Fork: once at start, whether the game or KC3's translations changed since the last start, each while its box is on
/// (설정 › 동작 › 번역 데이터, both off by default), raised as an alarm with the common alarm settings (알람 › 기본
/// 설정): the cue to start the translation work (the project's skills 번역갱신-라이브서버 and 번역갱신-KC3). Mockup
/// 20261007-update-watch-v1, approved 2026-10-07 "구현". The alarm shows its dialog even while the common settings
/// turn alarms off, and the first check's line shows in the log panel (user's decision 2026-10-14 "동작은 가와 나를
/// 적용": an update was missed with alarms off and nothing in the log).
/// <para>
/// The game version is VersionInfo.scriptVesion of the official gadget server's kcs_const.js, a public file fetched
/// once, apart from the live browser, as the game images are; KC3's is the newest commit of kc3-translations' data/.
/// The first check only keeps the value; a check that fails is logged and keeps the value it had.
/// </para>
/// </summary>
public static class ForkUpdateWatch
{
	private const string GameVersionUrl = "https://w00g.kancolle-server.com/gadget_html5/js/kcs_const.js";
	private const string Kc3CommitUrl = "https://api.github.com/repos/KC3Kai/kc3-translations/commits?path=data&per_page=1";

	private static ForkSettings.UpdateWatchSettings Settings => ForkSettings.Current.UpdateWatch;

	/// <summary>One notifier for both alarms: each one subscribes to the viewer's timer for good.</summary>
	private static NotifierBase? alarm;

	/// <summary>Called at start (FormMainViewModel.AddForkViews): checks once the main window has shown.</summary>
	public static void Install(System.Windows.Window window)
	{
		if (!Settings.Game && !Settings.Kc3) return;

		window.ContentRendered += OnShown;

		void OnShown(object? sender, EventArgs e)
		{
			window.ContentRendered -= OnShown;
			_ = CheckAsync();
		}
	}

	private static async Task CheckAsync()
	{
		using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };
		// GitHub's API asks for one
		client.DefaultRequestHeaders.UserAgent.ParseAdd("KCViewer");

		if (Settings.Game)
		{
			await Check(ForkStrings.UpdateWatchGame, () => GameVersion(client), Settings.GameVersion,
				value => Settings.GameVersion = value, ForkStrings.UpdateWatchGameMessage);
		}

		if (Settings.Kc3)
		{
			await Check(ForkStrings.UpdateWatchKc3, () => Kc3Commit(client), Settings.Kc3Commit,
				value => Settings.Kc3Commit = value, (old, now) => ForkStrings.UpdateWatchKc3Message(Short(old), Short(now)));
		}
	}

	private static async Task Check(string title, Func<Task<string>> fetch, string last, Action<string> keep, Func<string, string, string> message)
	{
		string value;
		try
		{
			value = await fetch();
		}
		catch (Exception e)
		{
			Utility.Logger.Add(2, ForkStrings.UpdateWatchFailed(title, e.Message.TrimEnd('.')));
			return;
		}

		if (value == last) return;

		await Application.Current.Dispatcher.InvokeAsync(() =>
		{
			keep(value);
			ForkSettings.Current.Save();

			if (last.Length == 0)
			{
				// shown in the log panel (its default level is 2), so a first check is seen to have kept a value
				Utility.Logger.Add(2, ForkStrings.UpdateWatchFirst(title, value));
				return;
			}

			string text = message(last, value);
			Utility.Logger.Add(2, $"{title}: {text}");

			// the common settings as they are now (they may have changed since the notifier was made)
			alarm ??= new Alarm(ForkNotifierDefaults.Config);
			ForkNotifierDefaults.ApplyTo(alarm, new ForkSettings.NotifierOverride());
			// rare and meant to be seen: on and with its dialog whatever the common settings say (their sound, image and
			// dialog look still apply)
			alarm.IsEnabled = true;
			alarm.IsSilenced = false;
			alarm.ShowsDialog = true;
			alarm.DialogData.Title = title;
			alarm.DialogData.Message = text;
			alarm.Notify();
		});
	}

	private static async Task<string> GameVersion(HttpClient client)
	{
		string script = await client.GetStringAsync(GameVersionUrl);
		Match match = Regex.Match(script, @"VersionInfo\.scriptVesion\s*=\s*""([^""]+)""");
		if (!match.Success) throw new InvalidOperationException("VersionInfo.scriptVesion");

		return match.Groups[1].Value;
	}

	private static async Task<string> Kc3Commit(HttpClient client)
	{
		using JsonDocument commits = JsonDocument.Parse(await client.GetStringAsync(Kc3CommitUrl));
		return commits.RootElement[0].GetProperty("sha").GetString() ?? throw new InvalidOperationException("sha");
	}

	/// <summary>A commit id as GitHub shows it.</summary>
	private static string Short(string sha) => sha.Length > 7 ? sha[..7] : sha;

	private sealed class Alarm(Utility.Configuration.ConfigurationData.ConfigNotifierBase config) : NotifierBase(config);
}
