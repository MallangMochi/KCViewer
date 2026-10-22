using KCViewer.Fork;

namespace KCViewer.Window.Wpf.Log;

/// <summary>
/// Fork: the log panel shows the newest <see cref="ForkLogCleanup.KeepLines"/> lines only, for long sessions (user
/// decision 2026-10-01). The viewer's own entries are cleaned up daily by <see cref="ForkLogCleanup"/>.
/// </summary>
public partial class LogViewModel
{
	private void ForkKeepNewest()
	{
		while (LogList.Count > ForkLogCleanup.KeepLines)
		{
			LogList.RemoveAt(0);
		}
	}
}
