using System;
using System.IO;
using System.Linq;
using KCViewer.Utility;

namespace KCViewer.Fork
{
	/// <summary>
	/// Fork: keeps the viewer's log from growing for as long as it runs (user decision 2026-10-01). Every day at the set
	/// hour (<see cref="ForkSettings.LogSettings.CleanupHour"/>, 05:00 by default, when the game's day resets) the entries
	/// older than the newest <see cref="KeepLines"/> are written to the log file, overwriting it, and dropped from
	/// memory. Closing the viewer overwrites the file with the entries left (<see cref="Logger.Save(string)"/>), more
	/// than <see cref="KeepLines"/> when the hour has not come since the last cleanup. The log panel keeps its own
	/// newest <see cref="KeepLines"/> lines (LogViewModel.Fork.cs).
	/// </summary>
	public static class ForkLogCleanup
	{
		public const int KeepLines = 1000;

		private static DateTime LastCheck { get; set; }

		public static void Install()
		{
			LastCheck = DateTime.Now;
			SystemEvents.UpdateTimerTick += Tick;
		}

		private static void Tick()
		{
			DateTime now = DateTime.Now;
			bool due = IsDue(LastCheck, now, ForkSettings.Current.Log.CleanupHourValue);
			LastCheck = now;

			if (due) Run();
		}

		/// <summary>Whether <paramref name="hour"/>:00 came after <paramref name="lastCheck"/> and by <paramref name="now"/>.</summary>
		public static bool IsDue(DateTime lastCheck, DateTime now, int hour)
		{
			DateTime due = now.Date.AddHours(hour);
			if (due > now) due = due.AddDays(-1);

			return lastCheck < due;
		}

		private static void Run()
		{
			int dropped = Logger.ForkDropOld(KeepLines, Logger.DefaultPath, Configuration.Config.Log.SaveLogFlag, out string? error);

			if (error is not null)
			{
				Logger.Add(3, ForkStrings.LogCleanupFailed(error));
			}
			else if (dropped > 0)
			{
				Logger.Add(1, ForkStrings.LogCleanedUp(dropped));
			}
		}
	}
}

namespace KCViewer.Utility
{
	public sealed partial class Logger
	{
		/// <summary>
		/// Fork: drops all but the newest <paramref name="keep"/> entries. With <paramref name="write"/>, the dropped
		/// entries of the logging level and above overwrite <paramref name="path"/> first; if that fails nothing is dropped.
		/// </summary>
		/// <returns>The number of entries dropped.</returns>
		public static int ForkDropOld(int keep, string path, bool write, out string? error)
		{
			error = null;

			lock (Instance)
			{
				int drop = Instance.log.Count - keep;
				if (drop <= 0) return 0;

				if (write)
				{
					try
					{
						using StreamWriter sw = new(path, false, Configuration.Config.Log.FileEncoding);
						int priority = Configuration.Config.Log.LogLevel;

						foreach (LogData l in Instance.log.Take(drop).Where(l => l.Priority >= priority))
						{
							sw.WriteLine(l.ToString());
						}
					}
					catch (Exception ex)
					{
						error = ex.Message;
						return 0;
					}
				}

				Instance.log.RemoveRange(0, drop);
				Instance.lastSavedCount = Math.Max(0, Instance.lastSavedCount - drop);
				return drop;
			}
		}
	}
}
