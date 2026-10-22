using System.Collections.Generic;
using System.Linq;
using KCViewer.Fork;

namespace KCViewer.Window.Settings.Log;

/// <summary>Fork: the hour of the daily log cleanup (<see cref="ForkLogCleanup"/>), in the fork's own settings.</summary>
public partial class ConfigurationLogViewModel
{
	public sealed record ForkHourOption(int Hour, string Display);

	public List<ForkHourOption> ForkCleanupHours { get; } = Enumerable.Range(0, 24)
		.Select(hour => new ForkHourOption(hour, $"{hour:00}:00"))
		.ToList();

	public int ForkCleanupHour { get; set; } = ForkSettings.Current.Log.CleanupHourValue;

	public string ForkCleanupHourLabel => ForkStrings.LogCleanupHour;
	public string ForkCleanupHourToolTip => ForkStrings.LogCleanupHourToolTip(ForkLogCleanup.KeepLines);

	private void ForkSave()
	{
		if (ForkSettings.Current.Log.CleanupHour == ForkCleanupHour) return;

		ForkSettings.Current.Log.CleanupHour = ForkCleanupHour;
		ForkSettings.Current.Save();
	}
}
