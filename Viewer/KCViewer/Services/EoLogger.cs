using System;
using KCViewer.Avalonia.Translation;
using KCViewer.Utility;

namespace KCViewer.Services;

public class EoLogger : IEoLogger
{
	public void Add(int level, string msg)
	{
		App.Current?.Dispatcher.Invoke(() =>
		{
			Logger.Add(level, msg);
		});
	}

	public void SendErrorReport(Exception ex, string message, string? connectionName = null, string? connectionData = null)
	{
		App.Current?.Dispatcher.Invoke(() =>
		{
			ErrorReporter.SendErrorReport(ex, message, connectionName, connectionData);
		});
	}
}
