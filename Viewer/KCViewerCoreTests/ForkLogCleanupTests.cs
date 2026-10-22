using System;
using System.IO;
using System.Linq;
using KCViewer.Fork;
using KCViewer.Utility;
using Xunit;

namespace KCViewerCoreTests;

/// <summary>Fork: the daily log cleanup (KCViewer/Fork/ForkLogCleanup.cs). The tests share the viewer's one log.</summary>
public class ForkLogCleanupTests
{
	private static readonly DateTime Day = new(2026, 10, 1);

	[Theory]
	[InlineData("04:59:59", "05:00:00", 5, true)]
	[InlineData("05:00:00", "05:00:01", 5, false)]
	[InlineData("05:00:01", "05:10:00", 5, false)]
	[InlineData("03:00:00", "04:59:59", 5, false)]
	[InlineData("-1 23:00:00", "06:00:00", 5, true)]
	[InlineData("-1 23:59:59", "00:00:01", 0, true)]
	[InlineData("-1 06:00:00", "04:00:00", 5, false)]
	[InlineData("-2 06:00:00", "04:00:00", 5, true)]
	public void IsDue(string lastCheck, string now, int hour, bool expected)
	{
		Assert.Equal(expected, ForkLogCleanup.IsDue(At(lastCheck), At(now), hour));
	}

	[Fact]
	public void DropOldWritesTheDroppedEntriesOverTheFile()
	{
		string path = Path.Combine(Path.GetTempPath(), $"fork-log-{Guid.NewGuid():N}.log");
		File.WriteAllText(path, "an earlier write\n");
		Configuration.Config.Log.LogLevel = 2;
		Configuration.Config.Log.SaveLogImmediately = false;

		try
		{
			Logger.Clear();
			for (int i = 0; i < 1500; i++)
			{
				Logger.Add(i % 3 == 0 ? 1 : 2, $"entry {i}");
			}

			int dropped = Logger.ForkDropOld(ForkLogCleanup.KeepLines, path, true, out string? error);

			Assert.Null(error);
			Assert.Equal(500, dropped);
			Assert.Equal(1000, Logger.Log.Count);
			Assert.EndsWith("entry 500", Logger.Log[0].Message);

			// Overwritten: only the dropped entries of the logging level (2) and above
			string[] lines = File.ReadAllLines(path);
			Assert.DoesNotContain(lines, line => line.Contains("an earlier write"));
			Assert.Equal(Enumerable.Range(0, 500).Count(i => i % 3 != 0), lines.Length);
			Assert.EndsWith(": entry 1", lines[0]);

			// Nothing more to drop
			Assert.Equal(0, Logger.ForkDropOld(ForkLogCleanup.KeepLines, path, true, out _));
			Assert.Equal(lines.Length, File.ReadAllLines(path).Length);

			// Closing the viewer overwrites the file with the entries left
			Logger.Save(path);
			string[] left = File.ReadAllLines(path);
			Assert.Equal(Enumerable.Range(500, 1000).Count(i => i % 3 != 0), left.Length);
			Assert.EndsWith(": entry 500", left[0]);
		}
		finally
		{
			Logger.Clear();
			File.Delete(path);
		}
	}

	[Fact]
	public void DropOldWithoutSavingOnlyDrops()
	{
		string path = Path.Combine(Path.GetTempPath(), $"fork-log-{Guid.NewGuid():N}.log");

		try
		{
			Logger.Clear();
			for (int i = 0; i < 1200; i++)
			{
				Logger.Add(2, $"entry {i}");
			}

			Assert.Equal(200, Logger.ForkDropOld(ForkLogCleanup.KeepLines, path, false, out string? error));
			Assert.Null(error);
			Assert.Equal(1000, Logger.Log.Count);
			Assert.False(File.Exists(path));
		}
		finally
		{
			Logger.Clear();
			File.Delete(path);
		}
	}

	/// <summary>"HH:mm:ss" on <see cref="Day"/>, or "-1 HH:mm:ss" a day earlier.</summary>
	private static DateTime At(string text)
	{
		string[] parts = text.Split(' ');
		int days = parts.Length == 2 ? int.Parse(parts[0]) : 0;
		return Day.AddDays(days).Add(TimeSpan.Parse(parts[^1]));
	}
}
